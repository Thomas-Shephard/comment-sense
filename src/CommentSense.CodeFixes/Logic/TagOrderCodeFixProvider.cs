using System.Collections.Immutable;
using System.Composition;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that reorders XML documentation tags to follow the standard order.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TagOrderCodeFixProvider)), Shared]
public class TagOrderCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId
    ];

    /// <inheritdoc />
    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.SortDocumentationTagsTitle,
                    createChangedDocument: c => FixOrderAsync(context.Document, diagnostic, c),
                    equivalenceKey: nameof(TagOrderCodeFixProvider)),
                diagnostic);
        }

        return Task.CompletedTask;
    }

    internal static async Task<Document> FixOrderAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = Guard.AgainstNull(await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));

        var node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true);
        var docTrivia = node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docTrivia is null)
            return document;

        var options = CommentSenseOptions.GetOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree);

        var originalContent = docTrivia.Content;
        var tagNodes = originalContent.Where(n => n is XmlElementSyntax or XmlEmptyElementSyntax).ToList();

        var sortedTags = tagNodes
            .OrderBy(t => GetTagPriority(t, options.TagOrder))
            .ToList();

        var resultList = originalContent.ToList();
        var tagIndices = new List<int>();
        for (int i = 0; i < originalContent.Count; i++)
        {
            if (originalContent[i] is XmlElementSyntax or XmlEmptyElementSyntax)
                tagIndices.Add(i);
        }

        for (int i = 0; i < tagIndices.Count; i++)
        {
            resultList[tagIndices[i]] = sortedTags[i];
        }

        var newDocTrivia = docTrivia.WithContent(SyntaxFactory.List(resultList));
        return document.WithSyntaxRoot(root.ReplaceNode(docTrivia, newDocTrivia));
    }

    internal static int GetTagPriority(XmlNodeSyntax tag, IReadOnlyDictionary<string, int> tagOrder)
    {
        var tagName = tag.GetTagName().ToLowerInvariant();
        if (string.IsNullOrEmpty(tagName))
            return 100;

        if (tagOrder.TryGetValue(tagName, out var priority))
            return priority;

        return 100;
    }
}
