using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that removes redundant or stray XML documentation elements.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RedundancyRemovalCodeFixProvider)), Shared]
public class RedundancyRemovalCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.StrayParameterDocumentationId,
        CommentSenseDiagnosticIds.StrayTypeParameterDocumentationId,
        CommentSenseDiagnosticIds.DuplicateParameterDocumentationId,
        CommentSenseDiagnosticIds.DuplicateTypeParameterDocumentationId,
        CommentSenseDiagnosticIds.StrayReturnValueDocumentationId,
        CommentSenseDiagnosticIds.StrayValueDocumentationId,
        CommentSenseDiagnosticIds.StraySummaryDocumentationId,
        CommentSenseDiagnosticIds.StrayExceptionDocumentationId
    ];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => new RedundancyRemovalFixAllProvider();

    private sealed class RedundancyRemovalFixAllProvider() : FixAllProviderBase(Resources.RemoveAllRedundantTitle)
    {
        internal override async Task<Document> FixDocumentInternalAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            if (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is not { } root)
                return document;

            var nodesToRemove = new HashSet<XmlNodeSyntax>();
            var xmlNodes = diagnostics
                .Select(d => FindXmlNode(root, d.Location.SourceSpan))
                .OfType<XmlNodeSyntax>();

            foreach (var xmlNode in xmlNodes)
            {
                nodesToRemove.Add(xmlNode);

                if (xmlNode.GetAssociatedWhitespaceToRemove() is { } whitespace)
                    nodesToRemove.Add(whitespace);
            }

            if (nodesToRemove.Count == 0)
                return document;

            var newRoot = root.RemoveNodes(nodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return newRoot is not null
                ? document.WithSyntaxRoot(newRoot)
                : document;
        }
    }

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        if (await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false) is not { } root)
            return;

        var targets = context.Diagnostics
            .Select(d => (Diagnostic: d, XmlNode: FindXmlNode(root, d.Location.SourceSpan)))
            .Where(x => x.XmlNode != null)
            .OfType<(Diagnostic, XmlNodeSyntax)>();

        foreach (var (diagnostic, xmlNode) in targets)
        {
            var tagName = xmlNode.GetTagName();
            var name = xmlNode.GetNameAttribute();

            var title = !string.IsNullOrEmpty(name)
                ? string.Format(CultureInfo.InvariantCulture, Resources.RemoveRedundantNamedTagTitle, tagName, name)
                : string.Format(CultureInfo.InvariantCulture, Resources.RemoveRedundantTagTitle, tagName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => FixDocumentAsync(context.Document, diagnostic, c),
                    equivalenceKey: nameof(RedundancyRemovalCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> FixDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var fixAllProvider = new RedundancyRemovalFixAllProvider();
        return await fixAllProvider.FixDocumentInternalAsync(document, [diagnostic], cancellationToken).ConfigureAwait(false);
    }
}
