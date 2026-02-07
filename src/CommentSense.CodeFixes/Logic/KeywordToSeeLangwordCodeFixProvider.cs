using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that replaces plain text keywords with &lt;see langword="..." /&gt;.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(KeywordToSeeLangwordCodeFixProvider)), Shared]
public class KeywordToSeeLangwordCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => [CommentSenseDiagnosticIds.UseLangwordId];

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var sourceText = await context.Document.GetTextAsync(context.CancellationToken).ConfigureAwait(false);
        var optionsProvider = context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
        var commentSenseOptions = CommentSenseOptions.GetOptions(optionsProvider, root.SyntaxTree);

        foreach (var diagnostic in context.Diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            var xmlText = FindXmlText(root, span);

            if (xmlText == null) continue;

            var keyword = sourceText.ToString(diagnostic.Location.SourceSpan);
            var canonicalKeyword = GetCanonicalKeyword(commentSenseOptions.Langwords, keyword);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: string.Format(CultureInfo.InvariantCulture, Resources.UseLangwordTitle, canonicalKeyword),
                    createChangedDocument: c => ConvertKeywordToSeeLangwordAsync(context.Document, diagnostic.Location.SourceSpan, c),
                    equivalenceKey: nameof(KeywordToSeeLangwordCodeFixProvider)),
                diagnostic);
        }
    }

    private static XmlTextSyntax? FindXmlText(SyntaxNode root, TextSpan span)
    {
        return root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true).FirstAncestorOrSelf<XmlTextSyntax>();
    }

    private static async Task<Document> ConvertKeywordToSeeLangwordAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var xmlText = FindXmlText(root, diagnosticSpan);
        if (xmlText == null) return document;

        var docTrivia = xmlText.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docTrivia == null) return document;

        var sourceText = await document.GetTextAsync(cancellationToken).ConfigureAwait(false);
        var keyword = sourceText.ToString(diagnosticSpan);

        var optionsProvider = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
        var commentSenseOptions = CommentSenseOptions.GetOptions(optionsProvider, root.SyntaxTree);

        var canonicalKeyword = GetCanonicalKeyword(commentSenseOptions.Langwords, keyword);
        var seeLangword = CreateSeeLangwordElement(canonicalKeyword);
        var replacementNodes = CreateReplacementNodes(xmlText, diagnosticSpan, seeLangword);

        var parent = xmlText.Parent;
        if (parent == null) return document;

        SyntaxNode? updatedParent = parent switch
        {
            XmlElementSyntax xmlElement => xmlElement.WithContent(xmlElement.Content.ReplaceRange(xmlText, replacementNodes)),
            DocumentationCommentTriviaSyntax docTriviaContent => docTriviaContent.WithContent(docTriviaContent.Content.ReplaceRange(xmlText, replacementNodes)),
            _ => null
        };

        if (updatedParent == null) return document;

        var newDocTrivia = parent == docTrivia
            ? (DocumentationCommentTriviaSyntax)updatedParent
            : docTrivia.ReplaceNode(parent, updatedParent);
        var oldTrivia = docTrivia.ParentTrivia;
        var token = oldTrivia.Token;

        // If the trivia is not attached to a token, try to find it via root
        if (token == default)
            token = root.FindToken(oldTrivia.SpanStart);

        if (token == default)
            return document;

        var newToken = token.ReplaceTrivia(oldTrivia, SyntaxFactory.Trivia(newDocTrivia));
        return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
    }

    internal static string GetCanonicalKeyword(IEnumerable<string> langwords, string keyword)
    {
        return langwords.FirstOrDefault(w => string.Equals(w, keyword, StringComparison.OrdinalIgnoreCase)) ?? keyword;
    }

    private static XmlEmptyElementSyntax CreateSeeLangwordElement(string keyword)
    {
        return SyntaxFactory.XmlEmptyElement(
            SyntaxFactory.XmlName("see"),
            [
                SyntaxFactory.XmlTextAttribute(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier("langword").WithLeadingTrivia(SyntaxFactory.Space)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(keyword)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken))
            ])
            .WithSlashGreaterThanToken(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Space), SyntaxKind.SlashGreaterThanToken, SyntaxTriviaList.Empty));
    }

    private static List<XmlNodeSyntax> CreateReplacementNodes(XmlTextSyntax xmlText, TextSpan diagnosticSpan, XmlEmptyElementSyntax seeLangword)
    {
        var tokenToSplit = xmlText.TextTokens.First(t => t.Span.Contains(diagnosticSpan));
        var tokenIndex = xmlText.TextTokens.IndexOf(tokenToSplit);

        var prefixText = tokenToSplit.Text.Substring(0, diagnosticSpan.Start - tokenToSplit.SpanStart);
        var suffixText = tokenToSplit.Text.Substring(diagnosticSpan.End - tokenToSplit.SpanStart);

        var replacementNodes = new List<XmlNodeSyntax>();

        // Prefix: Includes all tokens before tokenIndex, plus the prefixText of tokenIndex.
        var prefixTokens = new List<SyntaxToken>(xmlText.TextTokens.Take(tokenIndex));
        var finalSeeLangword = seeLangword;

        if (!string.IsNullOrEmpty(prefixText))
        {
            prefixTokens.Add(SyntaxFactory.XmlTextLiteral(tokenToSplit.LeadingTrivia, prefixText, prefixText, SyntaxTriviaList.Empty));
        }
        else
        {
            finalSeeLangword = finalSeeLangword.WithLeadingTrivia(tokenToSplit.LeadingTrivia);
        }

        if (prefixTokens.Count > 0)
            replacementNodes.Add(SyntaxFactory.XmlText(SyntaxFactory.TokenList(prefixTokens)));

        // Suffix: Includes the suffixText of tokenIndex, plus all tokens after tokenIndex.
        var suffixTokens = new List<SyntaxToken>();
        if (!string.IsNullOrEmpty(suffixText))
        {
            suffixTokens.Add(SyntaxFactory.XmlTextLiteral(SyntaxTriviaList.Empty, suffixText, suffixText, tokenToSplit.TrailingTrivia));
        }
        else
        {
            finalSeeLangword = finalSeeLangword.WithTrailingTrivia(tokenToSplit.TrailingTrivia);
        }

        replacementNodes.Add(finalSeeLangword);

        suffixTokens.AddRange(xmlText.TextTokens.Skip(tokenIndex + 1));

        if (suffixTokens.Count > 0)
            replacementNodes.Add(SyntaxFactory.XmlText(SyntaxFactory.TokenList(suffixTokens)));

        return replacementNodes;
    }
}
