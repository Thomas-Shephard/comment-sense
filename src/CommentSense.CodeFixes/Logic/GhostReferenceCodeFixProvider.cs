using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that wraps ghost references in &lt;paramref /&gt; or &lt;typeparamref /&gt;.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(GhostReferenceCodeFixProvider)), Shared]
public class GhostReferenceCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.GhostParameterReferenceId,
        CommentSenseDiagnosticIds.GhostTypeParameterReferenceId
    ];

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var span = diagnostic.Location.SourceSpan;
            var xmlText = FindXmlText(root, span);

            if (xmlText == null)
                continue;

            if (!diagnostic.Properties.TryGetValue("originalName", out var originalName) || originalName == null)
                continue;

            var isParam = diagnostic.Id == CommentSenseDiagnosticIds.GhostParameterReferenceId;
            var title = string.Format(
                CultureInfo.InvariantCulture,
                isParam ? Resources.WrapInParamrefTitle : Resources.WrapInTypeparamrefTitle,
                originalName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => WrapInReferenceAsync(context.Document, diagnostic, originalName, isParam, c),
                    equivalenceKey: nameof(GhostReferenceCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> WrapInReferenceAsync(Document document, Diagnostic diagnostic, string originalName, bool isParam, CancellationToken cancellationToken)
    {
        var diagnosticSpan = diagnostic.Location.SourceSpan;

        return await ReplaceTextWithNodesAsync(document, diagnosticSpan, (xmlText, tokenIndex, relativeStart, relativeEnd) =>
        {
            var tagName = isParam ? "paramref" : "typeparamref";
            var referenceElement = CreateReferenceElement(tagName, originalName);
            return CreateReplacementNodes(xmlText, tokenIndex, relativeStart, relativeEnd, referenceElement);
        }, cancellationToken).ConfigureAwait(false);
    }

    private static XmlEmptyElementSyntax CreateReferenceElement(string tagName, string name)
    {
        return SyntaxFactory.XmlEmptyElement(
            SyntaxFactory.XmlName(tagName),
            [
                SyntaxFactory.XmlNameAttribute(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier("name").WithLeadingTrivia(SyntaxFactory.Space)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    SyntaxFactory.IdentifierName(name),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken))
            ])
            .WithSlashGreaterThanToken(SyntaxFactory.Token(SyntaxTriviaList.Create(SyntaxFactory.Space), SyntaxKind.SlashGreaterThanToken, SyntaxTriviaList.Empty));
    }
}
