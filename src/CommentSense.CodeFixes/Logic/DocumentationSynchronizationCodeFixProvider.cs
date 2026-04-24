using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that renames stray XML documentation tags to match current code symbols via fuzzy matching.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DocumentationSynchronizationCodeFixProvider)), Shared]
public class DocumentationSynchronizationCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.MissingParameterDocumentationId,
        CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId,
        CommentSenseDiagnosticIds.StrayParameterDocumentationId,
        CommentSenseDiagnosticIds.StrayTypeParameterDocumentationId
    ];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => new DocumentationSynchronizationFixAllProvider();

    private sealed class DocumentationSynchronizationFixAllProvider() : FixAllProviderBase(Resources.RenameAllStrayTagsTitle)
    {
        internal override async Task<Document> FixDocumentInternalAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = Guard.AgainstNull(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));

            var semanticModel = Guard.AgainstNull(await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false));

            var options = CommentSenseOptionsLoader.GetOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree);

            var docCache = new Dictionary<ISymbol, (string? XML, System.Xml.Linq.XElement? Element)>(SymbolEqualityComparer.Default);
            var allMatches = new List<DocumentationSynchronizationLogic.MatchResult>();

            foreach (var diagnostic in diagnostics)
            {
                var match = await DocumentationSynchronizationLogic.FindMatchAsync(root, semanticModel, diagnostic, options, docCache, cancellationToken);
                if (match != null)
                    allMatches.Add(match.Value);
            }

            var consumedNodes = new HashSet<XmlNodeSyntax>();
            var consumedNames = new HashSet<(ISymbol, string)>();
            var renames = new Dictionary<XmlNodeSyntax, string>();

            foreach (var match in allMatches.OrderByDescending(m => m.Similarity))
            {
                if (consumedNodes.Contains(match.Node) || consumedNames.Contains((match.Symbol, match.NewName)))
                    continue;

                renames[match.Node] = match.NewName;
                consumedNodes.Add(match.Node);
                consumedNames.Add((match.Symbol, match.NewName));
            }

            var newRoot = root.ReplaceNodes(renames.Keys, (oldNode, newNode) =>
            {
                if (!renames.TryGetValue(oldNode, out var newName))
                    return newNode;

                return newNode switch
                {
                    XmlElementSyntax element => element.WithStartTag(DocumentationSynchronizationLogic.RenameAttribute(element.StartTag, newName)),
                    XmlEmptyElementSyntax emptyElement => emptyElement.WithAttributes(DocumentationSynchronizationLogic.RenameAttribute(emptyElement.Attributes, newName)),
                    _ => newNode
                };
            });

            return document.WithSyntaxRoot(newRoot);
        }
    }

    /// <inheritdoc />
    public override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = Guard.AgainstNull(await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false));

        var semanticModel = Guard.AgainstNull(await context.Document.GetSemanticModelAsync(context.CancellationToken).ConfigureAwait(false));

        var options = CommentSenseOptionsLoader.GetOptions(context.Document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree);

        foreach (var diagnostic in context.Diagnostics)
        {
            var match = await DocumentationSynchronizationLogic.FindMatchAsync(root, semanticModel, diagnostic, options, null, context.CancellationToken);
            if (match == null)
                continue;

            var title = string.Format(CultureInfo.InvariantCulture, Resources.RenameStrayTagTitle, match.Value.TagName, match.Value.OldName, match.Value.NewName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => FixDocumentAsync(context.Document, match.Value, c),
                    equivalenceKey: nameof(DocumentationSynchronizationCodeFixProvider)),
                diagnostic);
        }
    }

    private static async Task<Document> FixDocumentAsync(Document document, DocumentationSynchronizationLogic.MatchResult match, CancellationToken cancellationToken)
    {
        var root = Guard.AgainstNull(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));

        XmlNodeSyntax? newNode = match.Node switch
        {
            XmlElementSyntax element => element.WithStartTag(DocumentationSynchronizationLogic.RenameAttribute(element.StartTag, match.NewName)),
            XmlEmptyElementSyntax emptyElement => emptyElement.WithAttributes(DocumentationSynchronizationLogic.RenameAttribute(emptyElement.Attributes, match.NewName)),
            _ => null
        };

        if (newNode is null)
            return document;

        return document.WithSyntaxRoot(root.ReplaceNode(match.Node, newNode));
    }
}
