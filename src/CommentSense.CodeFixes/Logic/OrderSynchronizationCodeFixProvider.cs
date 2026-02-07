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

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that reorders XML documentation tags to match the symbol signature.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(OrderSynchronizationCodeFixProvider)), Shared]
public class OrderSynchronizationCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.ParameterOrderMismatchId,
        CommentSenseDiagnosticIds.TypeParameterOrderMismatchId
    ];

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        var registeredTags = new HashSet<(DocumentationCommentTriviaSyntax, string)>();

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
            var xmlNode = node.FirstAncestorOrSelf<XmlNodeSyntax>(n => n is XmlElementSyntax or XmlEmptyElementSyntax);
            if (xmlNode is null)
                continue;

            var docTrivia = xmlNode.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
            if (docTrivia == null)
                continue;

            var tagName = xmlNode.GetTagName();
            if (!registeredTags.Add((docTrivia, tagName)))
                continue;

            var title = string.Format(CultureInfo.InvariantCulture, Resources.SortTagsTitle, tagName);

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => FixOrderAsync(context.Document, diagnostic, c),
                    equivalenceKey: $"{nameof(OrderSynchronizationCodeFixProvider)}_{tagName}"),
                diagnostic);
        }
    }

    internal static async Task<Document> FixOrderAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
        var xmlNode = node.FirstAncestorOrSelf<XmlNodeSyntax>(n => n is XmlElementSyntax or XmlEmptyElementSyntax);
        if (xmlNode == null) return document;

        var docTrivia = xmlNode.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docTrivia == null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel == null) return document;

        var memberDeclaration = docTrivia.ParentTrivia.Token.Parent?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDeclaration == null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(memberDeclaration, cancellationToken);
        if (symbol == null) return document;

        var tagName = xmlNode.GetTagName();
        var expectedOrder = GetExpectedOrder(symbol, tagName);

        return ReorderTags(document, root, docTrivia, tagName, expectedOrder);
    }

    internal static ImmutableArray<string> GetExpectedOrder(ISymbol symbol, string tagName)
    {
        if (tagName == "param")
        {
            var parameters = symbol.GetParameters();
            if (!parameters.IsEmpty) return [.. parameters.Select(p => p.Name)];
        }
        else if (tagName == "typeparam")
        {
            var typeParameters = symbol.GetTypeParameters();
            if (!typeParameters.IsEmpty) return [.. typeParameters.Select(p => p.Name)];
        }

        return [];
    }

    internal static Document ReorderTags(Document document, SyntaxNode root, DocumentationCommentTriviaSyntax docTrivia, string tagName, ImmutableArray<string> expectedOrder)
    {
        var parentToTagNodes = CollectTagNodes(docTrivia, tagName);
        var parentsToUpdate = parentToTagNodes.Where(kvp => kvp.Value.Count > 1).Select(kvp => kvp.Key).ToList();

        if (parentsToUpdate.Count == 0)
            return document;

        var newRoot = root.ReplaceNodes(parentsToUpdate, (original, rewritten) =>
        {
            var tagNodes = parentToTagNodes[original];
            return ReorderContent(rewritten, tagNodes, expectedOrder);
        });

        return document.WithSyntaxRoot(newRoot);
    }

    private static Dictionary<SyntaxNode, List<(XmlNodeSyntax Node, string Name, int Index)>> CollectTagNodes(DocumentationCommentTriviaSyntax docTrivia, string tagName)
    {
        var parentToTagNodes = new Dictionary<SyntaxNode, List<(XmlNodeSyntax Node, string Name, int Index)>>();
        foreach (var xmlNode in docTrivia.DescendantNodes().OfType<XmlNodeSyntax>())
        {
            if (xmlNode.GetTagName() != tagName)
                continue;

            var name = xmlNode.GetNameAttribute();
            if (name is null)
                continue;

            var (parent, content) = xmlNode.GetParentContent();
            if (parent is null || content.Count == 0)
                continue;

            var index = content.IndexOf(xmlNode);
            if (index < 0)
                continue;

            if (!parentToTagNodes.TryGetValue(parent, out var list))
            {
                list = [];
                parentToTagNodes[parent] = list;
            }
            list.Add((xmlNode, name, index));
        }
        return parentToTagNodes;
    }

    private static SyntaxNode ReorderContent(SyntaxNode parent, List<(XmlNodeSyntax Node, string Name, int Index)> tagNodes, ImmutableArray<string> expectedOrder)
    {
        SyntaxList<XmlNodeSyntax> content = parent is XmlElementSyntax e
            ? e.Content
            : ((DocumentationCommentTriviaSyntax)parent).Content;

        var nodesByName = tagNodes
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .ToDictionary(
                g => g.Key,
                g => new Queue<XmlNodeSyntax>(g.Select(x => content[x.Index])),
                StringComparer.Ordinal);

        var originalNamesInOrder = tagNodes
            .OrderBy(x => x.Index)
            .Select(x => x.Name)
            .ToList();

        var expectedOrderIndices = expectedOrder
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        var sortedNames = originalNamesInOrder
            .Where(expectedOrderIndices.ContainsKey)
            .OrderBy(name => expectedOrderIndices[name])
            .ToList();

        var strayNames = originalNamesInOrder
            .Where(name => !expectedOrderIndices.ContainsKey(name))
            .ToList();

        var finalOrderNames = sortedNames.Concat(strayNames).ToList();

        var newContentList = content.ToList();
        for (int i = 0; i < tagNodes.Count; i++)
        {
            var targetName = finalOrderNames[i];
            var targetIndex = tagNodes[i].Index;
            newContentList[targetIndex] = nodesByName[targetName].Dequeue();
        }

        var newContent = SyntaxFactory.List(newContentList);
        if (parent is XmlElementSyntax e2)
            return e2.WithContent(newContent);

        return ((DocumentationCommentTriviaSyntax)parent).WithContent(newContent);
    }
}
