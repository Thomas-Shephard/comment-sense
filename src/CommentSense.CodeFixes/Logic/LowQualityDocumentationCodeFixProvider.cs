using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix for low-quality documentation (e.g., missing capitalization or ending punctuation).
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LowQualityDocumentationCodeFixProvider)), Shared]
public class LowQualityDocumentationCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.LowQualityDocumentationId
    ];

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
            return;

        var options = CommentSenseOptions.GetOptions(context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree);

        var diagnosticsWithElements = from diagnostic in context.Diagnostics
                                      let node = FindXmlNode(root, diagnostic.Location.SourceSpan)
                                      where node is XmlElementSyntax
                                      select (Diagnostic: diagnostic, Element: (XmlElementSyntax)node);

        foreach (var (diagnostic, xmlElement) in diagnosticsWithElements)
        {
            var tagName = xmlElement.StartTag.Name.LocalName.ValueText;

            if (FixElement(xmlElement, options) == xmlElement)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: string.Format(CultureInfo.InvariantCulture, Resources.ImproveDocumentationQualityTitle, tagName),
                    createChangedDocument: c => FixDocumentAsync(context.Document, xmlElement.Span, options, c),
                    equivalenceKey: nameof(LowQualityDocumentationCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> FixDocumentAsync(Document document, TextSpan span, CommentSenseOptions options, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null || FindXmlNode(root, span) is not XmlElementSyntax xmlElement)
            return document;

        var updatedElement = FixElement(xmlElement, options);
        return updatedElement == xmlElement ? document : document.WithSyntaxRoot(root.ReplaceNode(xmlElement, updatedElement));
    }

    private static XmlElementSyntax FixElement(XmlElementSyntax xmlElement, CommentSenseOptions options)
    {
        var content = xmlElement.Content;
        if (content.Count == 0)
            return xmlElement;

        var newContent = content;

        if (options.RequireCapitalization)
            newContent = CapitalizeFirstLetter(newContent);

        if (options.RequireEndingPunctuation)
            newContent = AddMissingPeriod(newContent);

        return newContent == content
            ? xmlElement
            : xmlElement.WithContent(newContent);
    }

    private static SyntaxList<XmlNodeSyntax> CapitalizeFirstLetter(SyntaxList<XmlNodeSyntax> content)
    {
        foreach (XmlNodeSyntax node in content)
        {
            if (TryProcessNodeForCapitalization(node, content, out var newContent, out var shouldStop))
                return newContent;

            if (shouldStop)
                return content;
        }

        return content;
    }

    private static bool TryProcessNodeForCapitalization(XmlNodeSyntax node, SyntaxList<XmlNodeSyntax> content, out SyntaxList<XmlNodeSyntax> newContent, out bool shouldStop)
    {
        newContent = default;
        shouldStop = false;

        switch (node)
        {
            case XmlTextSyntax or XmlCDataSectionSyntax:
                {
                    if (!TryCapitalizeTokens(node.GetTextTokens(), out var newTokens, out shouldStop))
                        return false;

                    newContent = content.Replace(node, node.WithTextTokens(newTokens));
                    return true;
                }
            case XmlElementSyntax xmlElement:
                {
                    var newElementContent = CapitalizeFirstLetter(xmlElement.Content);
                    if (newElementContent != xmlElement.Content)
                    {
                        newContent = content.Replace(xmlElement, xmlElement.WithContent(newElementContent));
                        return true;
                    }

                    if (xmlElement.Content.HasAnyLetter())
                        shouldStop = true;
                    break;
                }
        }

        return false;
    }

    private static bool TryCapitalizeTokens(SyntaxTokenList tokens, out SyntaxTokenList newTokens, out bool shouldStop)
    {
        newTokens = default;
        shouldStop = false;

        for (int j = 0; j < tokens.Count; j++)
        {
            var token = tokens[j];
            var text = token.Text;
            var valueText = token.ValueText;

            for (int k = 0; k < valueText.Length; k++)
            {
                if (char.IsWhiteSpace(valueText[k]))
                    continue;

                if (!char.IsLetter(valueText[k]) || char.IsUpper(valueText[k]))
                {
                    shouldStop = true;
                    return false;
                }

                string newText;
                string newValueText;

                if (text == valueText)
                {
                    newText = text.Substring(0, k) + char.ToUpperInvariant(text[k]) + text.Substring(k + 1);
                    newValueText = newText;
                }
                else
                {
                    newValueText = valueText.Substring(0, k) + char.ToUpperInvariant(valueText[k]) + valueText.Substring(k + 1);
                    newText = newValueText;
                }

                var newToken = SyntaxFactory.XmlTextLiteral(token.LeadingTrivia, newText, newValueText, token.TrailingTrivia);
                newTokens = tokens.Replace(token, newToken);
                return true;
            }
        }

        return false;
    }

    private static SyntaxList<XmlNodeSyntax> AddMissingPeriod(SyntaxList<XmlNodeSyntax> content)
    {
        // Scan backwards to find the last meaningful text or tag
        for (int i = content.Count - 1; i >= 0; i--)
        {
            var node = content[i];
            if (TryProcessNodeForPunctuation(node, i, content, out var newContent, out var shouldStop))
                return newContent;

            if (shouldStop)
                return content;
        }

        return content;
    }

    private static bool TryProcessNodeForPunctuation(XmlNodeSyntax node, int index, SyntaxList<XmlNodeSyntax> content, out SyntaxList<XmlNodeSyntax> newContent, out bool shouldStop)
    {
        newContent = default;
        shouldStop = false;

        if (node is XmlTextSyntax or XmlCDataSectionSyntax)
        {
            if (!TryAddPeriodToTokens(node.GetTextTokens(), out var newTokens, out shouldStop))
                return false;

            newContent = content.Replace(node, node.WithTextTokens(newTokens));
            return true;
        }

        if (node is XmlCommentSyntax)
            return false;

        var state = node.GetPunctuationState();
        switch (state)
        {
            case DocumentationQualityExtensions.PunctuationState.Yes:
                shouldStop = true;
                return false;
            case DocumentationQualityExtensions.PunctuationState.No:
                newContent = content.Insert(index + 1, SyntaxFactory.XmlText(SyntaxFactory.XmlTextLiteral(".")));
                return true;
            case DocumentationQualityExtensions.PunctuationState.Meaningless:
            default:
                return false;
        }
    }

    private static bool TryAddPeriodToTokens(SyntaxTokenList tokens, out SyntaxTokenList newTokens, out bool shouldStop)
    {
        newTokens = default;
        shouldStop = false;

        for (int j = tokens.Count - 1; j >= 0; j--)
        {
            var token = tokens[j];
            var text = token.Text;
            var valueText = token.ValueText;
            var trimmedValueText = valueText.TrimEnd();
            if (trimmedValueText.Length == 0)
                continue;

            var lastChar = trimmedValueText[trimmedValueText.Length - 1];
            if (lastChar is '.' or '!' or '?')
            {
                shouldStop = true;
                return false;
            }

            var trimmedTextLength = text.TrimEnd().Length;
            var newText = text.Insert(trimmedTextLength, ".");
            var newValueText = valueText.Insert(trimmedValueText.Length, ".");
            var newToken = SyntaxFactory.XmlTextLiteral(token.LeadingTrivia, newText, newValueText, token.TrailingTrivia);
            newTokens = tokens.Replace(token, newToken);
            return true;
        }

        return false;
    }
}
