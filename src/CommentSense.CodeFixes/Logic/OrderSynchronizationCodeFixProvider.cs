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
        var root = Guard.AgainstNull(await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false));

        var registeredTags = new HashSet<(DocumentationCommentTriviaSyntax, string)>();

        foreach (var diagnostic in context.Diagnostics)
        {
            var xmlNode = FindXmlNode(root, diagnostic.Location.SourceSpan);
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
        var root = Guard.AgainstNull(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));

        var xmlNode = FindXmlNode(root, diagnostic.Location.SourceSpan);
        if (xmlNode == null) return document;

        var docTrivia = Guard.AgainstNull(xmlNode.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>());

        var semanticModel = Guard.AgainstNull(await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false));

        var memberDeclaration = GetMemberDeclaration(docTrivia);
        return Guard.WhenNotNull(memberDeclaration, declaration =>
        {
            var symbol = semanticModel.GetDeclaredSymbol(declaration, cancellationToken);
            if (symbol is null)
                return document;

            var tagName = xmlNode.GetTagName();
            var expectedOrder = symbol.GetExpectedMemberNames(tagName);

            return ReorderTags(document, root, docTrivia, tagName, expectedOrder);
        }, document);
    }

    internal static SyntaxNode? GetTriviaParent(DocumentationCommentTriviaSyntax docTrivia) => docTrivia.ParentTrivia.Token.Parent;

    internal static MemberDeclarationSyntax? GetMemberDeclaration(DocumentationCommentTriviaSyntax docTrivia) => GetTriviaParent(docTrivia)?.FirstAncestorOrSelf<MemberDeclarationSyntax>();

    internal static Document ReorderTags(Document document, SyntaxNode root, DocumentationCommentTriviaSyntax docTrivia, string tagName, ImmutableArray<string> expectedOrder)
    {
        var content = docTrivia.Content;
        var tagNodes = content
            .Select((node, index) => (Node: node, Name: node.GetNameAttribute(), Index: index))
            .Where(tag => tag.Node.GetTagName() == tagName && tag.Name != null)
            .ToList();

        if (tagNodes.Count <= 1)
            return document;

        var expectedOrderIndices = expectedOrder
            .Select((name, index) => (name, index))
            .ToDictionary(x => x.name, x => x.index, StringComparer.Ordinal);

        var sortedNodes = tagNodes
            .OrderBy(tag => expectedOrderIndices.TryGetValue(tag.Name!, out var index) ? index : int.MaxValue)
            .Select(tag => tag.Node)
            .ToList();

        var newContent = content.ToList();
        for (int i = 0; i < tagNodes.Count; i++)
            newContent[tagNodes[i].Index] = sortedNodes[i];

        var updatedTrivia = docTrivia.WithContent(SyntaxFactory.List(newContent));
        return document.WithSyntaxRoot(root.ReplaceNode(docTrivia, updatedTrivia));
    }
}
