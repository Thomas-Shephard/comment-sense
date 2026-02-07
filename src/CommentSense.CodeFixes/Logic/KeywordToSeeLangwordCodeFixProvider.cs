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

    private static async Task<Document> ConvertKeywordToSeeLangwordAsync(Document document, TextSpan diagnosticSpan, CancellationToken cancellationToken)
    {
        return await ReplaceTextWithNodesAsync(document, diagnosticSpan, (xmlText, tokenIndex, relativeStart, relativeEnd) =>
        {
            var tokenToSplit = xmlText.TextTokens[tokenIndex];
            var keyword = tokenToSplit.Text.Substring(relativeStart, relativeEnd - relativeStart);

            var optionsProvider = document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider;
            var commentSenseOptions = CommentSenseOptions.GetOptions(optionsProvider, xmlText.SyntaxTree);
            var canonicalKeyword = GetCanonicalKeyword(commentSenseOptions.Langwords, keyword);
            var seeLangword = CreateSeeLangwordElement(canonicalKeyword);
            return CreateReplacementNodes(xmlText, tokenIndex, relativeStart, relativeEnd, seeLangword);
        }, cancellationToken).ConfigureAwait(false);
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
}
