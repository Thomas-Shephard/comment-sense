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

        foreach (var diagnostic in context.Diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            var xmlText = FindXmlText(root, span);

            if (xmlText == null) continue;

            if (!diagnostic.Properties.TryGetValue("canonical", out var canonicalKeyword) || canonicalKeyword == null)
                continue;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: string.Format(CultureInfo.InvariantCulture, Resources.UseLangwordTitle, canonicalKeyword),
                    createChangedDocument: c => ConvertKeywordToSeeLangwordAsync(context.Document, diagnostic.Location.SourceSpan, canonicalKeyword, c),
                    equivalenceKey: nameof(KeywordToSeeLangwordCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> ConvertKeywordToSeeLangwordAsync(Document document, TextSpan diagnosticSpan, string canonicalKeyword, CancellationToken cancellationToken)
    {
        return await ReplaceTextWithNodesAsync(document, diagnosticSpan, (xmlText, tokenIndex, relativeStart, relativeEnd) => {
            var seeLangword = CreateSeeLangwordElement(canonicalKeyword);
            return CreateReplacementNodes(xmlText, tokenIndex, relativeStart, relativeEnd, seeLangword);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static XmlEmptyElementSyntax CreateSeeLangwordElement(string keyword)
    {
        return SyntaxFactory.XmlEmptyElement(
            SyntaxFactory.XmlName(DocumentationTags.See),
            [
                SyntaxFactory.XmlTextAttribute(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier(DocumentationAttributes.Langword).WithLeadingTrivia(SyntaxFactory.Space)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(keyword)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken))
            ])
            .WithSlashGreaterThanToken(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Space), SyntaxKind.SlashGreaterThanToken, SyntaxTriviaList.Empty));
    }
}
