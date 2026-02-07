using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

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
        CommentSenseDiagnosticIds.StrayValueDocumentationId
    ];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => new RedundancyRemovalFixAllProvider();

    internal sealed class RedundancyRemovalFixAllProvider : FixAllProvider
    {
        public override Task<CodeAction?> GetFixAsync(FixAllContext fixAllContext)
        {
            return Task.FromResult(GetFixInternalAsync(fixAllContext.Scope, fixAllContext));
        }

        internal static CodeAction? GetFixInternalAsync(FixAllScope scope, FixAllContext fixAllContext)
        {
            return scope switch
            {
                FixAllScope.Document when fixAllContext.Document != null => CodeAction.Create(Resources.RemoveAllRedundantTitle, ct => FixDocumentAsync(fixAllContext.Document, fixAllContext, ct)),
                FixAllScope.Project => CodeAction.Create(Resources.RemoveAllRedundantTitle, ct => FixProjectAsync(fixAllContext.Project, fixAllContext, ct)),
                FixAllScope.Solution => CodeAction.Create(Resources.RemoveAllRedundantTitle, ct => FixSolutionAsync(fixAllContext.Solution, fixAllContext, ct)),
                _ => null
            };
        }

        private static async Task<Document> FixDocumentAsync(Document document, FixAllContext fixAllContext, CancellationToken cancellationToken)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            return diagnostics.IsEmpty
                ? document
                : await FixDocumentInternalAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Solution> FixProjectAsync(Project project, FixAllContext fixAllContext, CancellationToken cancellationToken)
        {
            var newSolution = project.Solution;

            foreach (var document in project.Documents)
            {
                var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
                if (diagnostics.IsEmpty) continue;

                var fixedDocument = await FixDocumentInternalAsync(document, diagnostics, cancellationToken).ConfigureAwait(false);
                if (await fixedDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false) is { } fixedRoot)
                    newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, fixedRoot);
            }

            return newSolution;
        }

        private static async Task<Solution> FixSolutionAsync(Solution solution, FixAllContext fixAllContext, CancellationToken cancellationToken)
        {
            var newSolution = solution;

            foreach (var projectId in solution.Projects.Select(p => p.Id))
            {
                if (newSolution.GetProject(projectId) is { } currentProject)
                    newSolution = await FixProjectAsync(currentProject, fixAllContext, cancellationToken).ConfigureAwait(false);
            }

            return newSolution;
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
                    createChangedDocument: c => FixDocumentInternalAsync(context.Document, [diagnostic], c),
                    equivalenceKey: nameof(RedundancyRemovalCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> FixDocumentInternalAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
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

    private static XmlNodeSyntax? FindXmlNode(SyntaxNode root, TextSpan span)
    {
        var node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);
        return node.FirstAncestorOrSelf<XmlNodeSyntax>(n => n is XmlElementSyntax or XmlEmptyElementSyntax);
    }
}
