using System.Collections.Immutable;
using System.Composition;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CommentSense.Analyzers;

namespace CommentSense.CodeFixes;

[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CommentSenseCodeFixProvider)), Shared]
public class CommentSenseCodeFixProvider : CodeFixProvider
{
    private const string MissingDocumentationId = CommentSenseAnalyzer.MissingDocumentationId;
    private const string MissingParameterDocumentationId = CommentSenseAnalyzer.MissingParameterDocumentationId;
    private const string StrayParameterDocumentationId = CommentSenseAnalyzer.StrayParameterDocumentationId;
    private const string MissingTypeParameterDocumentationId = CommentSenseAnalyzer.MissingTypeParameterDocumentationId;
    private const string StrayTypeParameterDocumentationId = CommentSenseAnalyzer.StrayTypeParameterDocumentationId;
    private const string MissingReturnValueDocumentationId = CommentSenseAnalyzer.MissingReturnValueDocumentationId;

    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        MissingDocumentationId,
        MissingParameterDocumentationId,
        StrayParameterDocumentationId,
        MissingTypeParameterDocumentationId,
        StrayTypeParameterDocumentationId,
        MissingReturnValueDocumentationId
    ];

    public sealed override FixAllProvider GetFixAllProvider()
    {
        return WellKnownFixAllProviders.BatchFixer;
    }

    public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        foreach (var diagnostic in context.Diagnostics)
        {
            switch (diagnostic.Id)
            {
                case MissingDocumentationId:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Add documentation",
                            createChangedDocument: c => AddDocumentationAsync(context.Document, diagnostic, c),
                            equivalenceKey: MissingDocumentationId),
                        diagnostic);
                    break;
                case MissingParameterDocumentationId:
                case MissingTypeParameterDocumentationId:
                case MissingReturnValueDocumentationId:
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Update documentation",
                            createChangedDocument: c => UpdateDocumentationAsync(context.Document, diagnostic, c),
                            equivalenceKey: diagnostic.Id + (diagnostic.Properties.TryGetValue("Name", out var name) ? name : "")),
                        diagnostic);
                    break;
                case StrayParameterDocumentationId:
                case StrayTypeParameterDocumentationId:
                     context.RegisterCodeFix(
                        CodeAction.Create(
                            title: "Remove stray documentation",
                            createChangedDocument: c => RemoveStrayDocumentationAsync(context.Document, diagnostic, c),
                            equivalenceKey: diagnostic.Id + (diagnostic.Properties.TryGetValue("Name", out var strayName) ? strayName : "")),
                        diagnostic);
                    break;
            }
        }

        return Task.CompletedTask;
    }

    private static async Task<Document> AddDocumentationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var memberNode = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberNode is null) return document;

        var leadingTrivia = memberNode.GetLeadingTrivia();
        var indentation = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();
        var newLine = "\n";

        var comment = $"/// <summary>{newLine}{indentation}/// Summary.{newLine}{indentation}/// </summary>{newLine}{indentation}";
        var commentTrivia = SyntaxFactory.ParseLeadingTrivia(comment);

        var newTrivia = leadingTrivia.AddRange(commentTrivia);
        var newNode = memberNode.WithLeadingTrivia(newTrivia);

        return document.WithSyntaxRoot(root.ReplaceNode(memberNode, newNode));
    }

    private static async Task<Document> UpdateDocumentationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var memberNode = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberNode is null) return document;

        var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
        if (semanticModel is null) return document;

        var symbol = semanticModel.GetDeclaredSymbol(memberNode, cancellationToken);
        if (symbol is null) return document;

        var documentationTrivia = GetDocumentationCommentTrivia(memberNode);
        if (documentationTrivia == default) return document;

        var documentation = (DocumentationCommentTriviaSyntax)documentationTrivia.GetStructure()!;
        var indentation = memberNode.GetLeadingTrivia().LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia)).ToString();

        XmlNodeSyntax? newTag = null;
        string? targetName = null;
        switch (diagnostic.Id)
        {
            case MissingParameterDocumentationId:
                if (diagnostic.Properties.TryGetValue("Name", out targetName) && targetName is not null)
                {
                    newTag = CreateXmlElement("param", targetName, "Summary.", indentation);
                }
                break;
            case MissingTypeParameterDocumentationId:
                if (diagnostic.Properties.TryGetValue("Name", out targetName) && targetName is not null)
                {
                    newTag = CreateXmlElement("typeparam", targetName, "Summary.", indentation);
                }
                break;
            case MissingReturnValueDocumentationId:
                newTag = CreateXmlElement("returns", null, "Summary.", indentation);
                break;
        }

        if (newTag is null) return document;

        var content = documentation.Content;
        var insertIndex = GetInsertIndex(content, diagnostic.Id, targetName, symbol);

        var newContent = content.Insert(insertIndex, newTag);
        var newDocumentation = documentation.WithContent(newContent);

        var newTriviaList = memberNode.GetLeadingTrivia().Replace(documentationTrivia, SyntaxFactory.Trivia(newDocumentation));
        var newNode = memberNode.WithLeadingTrivia(newTriviaList);

        return document.WithSyntaxRoot(root.ReplaceNode(memberNode, newNode));
    }

    private static int GetInsertIndex(SyntaxList<XmlNodeSyntax> content, string diagnosticId, string? targetName, ISymbol symbol)
    {
        var typeParameters = symbol is INamedTypeSymbol nt ? nt.TypeParameters : (symbol is IMethodSymbol m ? m.TypeParameters : ImmutableArray<ITypeParameterSymbol>.Empty);
        var parameters = symbol is IMethodSymbol ms ? ms.Parameters : ImmutableArray<IParameterSymbol>.Empty;

        int GetOrderIndex(XmlElementSyntax element)
        {
            var tagName = element.StartTag.Name.LocalName.ValueText;
            var name = element.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault()?.Identifier.Identifier.ValueText;

            if (tagName == "summary") return 0;
            if (tagName == "typeparam")
            {
                var idx = IndexOf(typeParameters, p => p.Name == name);
                return idx != -1 ? 10 + idx : 10;
            }
            if (tagName == "param")
            {
                var idx = IndexOf(parameters, p => p.Name == name);
                return idx != -1 ? 100 + idx : 100;
            }
            if (tagName == "returns") return 1000;
            if (tagName == "exception") return 2000;
            return 3000;
        }

        int targetOrder;
        if (diagnosticId == MissingTypeParameterDocumentationId)
        {
            var idx = IndexOf(typeParameters, p => p.Name == targetName);
            targetOrder = idx != -1 ? 10 + idx : 10;
        }
        else if (diagnosticId == MissingParameterDocumentationId)
        {
            var idx = IndexOf(parameters, p => p.Name == targetName);
            targetOrder = idx != -1 ? 100 + idx : 100;
        }
        else if (diagnosticId == MissingReturnValueDocumentationId)
        {
            targetOrder = 1000;
        }
        else
        {
            return content.Count;
        }

        for (int i = 0; i < content.Count; i++)
        {
            if (content[i] is XmlElementSyntax element)
            {
                if (GetOrderIndex(element) > targetOrder)
                {
                    // Found a tag that should come after. Insert before it.
                    // But we want to insert before the preceding newline if possible.
                    if (i > 0 && content[i - 1] is XmlTextSyntax text && text.TextTokens.Any(t => t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken)))
                    {
                        return i - 1;
                    }
                    return i;
                }
            }
        }

        // If no tag found that comes after, insert before the last newline (which is before the member declaration)
        for (int i = content.Count - 1; i >= 0; i--)
        {
            if (content[i] is XmlTextSyntax xmlText && xmlText.TextTokens.Any(t => t.IsKind(SyntaxKind.XmlTextLiteralNewLineToken)))
            {
                return i;
            }
        }

        return content.Count;
    }

    private static int IndexOf<T>(ImmutableArray<T> array, Func<T, bool> predicate)
    {
        for (int i = 0; i < array.Length; i++)
        {
            if (predicate(array[i])) return i;
        }
        return -1;
    }

    private static async Task<Document> RemoveStrayDocumentationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root is null) return document;

        var node = root.FindNode(diagnostic.Location.SourceSpan);
        var memberNode = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberNode is null) return document;

        var documentationTrivia = GetDocumentationCommentTrivia(memberNode);
        if (documentationTrivia == default) return document;

        var documentation = (DocumentationCommentTriviaSyntax)documentationTrivia.GetStructure()!;

        if (!diagnostic.Properties.TryGetValue("Name", out var strayName) || strayName is null)
            return document;

        var tagToRemove = documentation.Content
            .OfType<XmlElementSyntax>()
            .FirstOrDefault(e => (e.StartTag.Name.LocalName.ValueText == "param" || e.StartTag.Name.LocalName.ValueText == "typeparam") &&
                                 e.StartTag.Attributes.OfType<XmlNameAttributeSyntax>().Any(a => a.Identifier.Identifier.ValueText == strayName));

        if (tagToRemove is null) return document;

        var index = documentation.Content.IndexOf(tagToRemove);
        var newContent = documentation.Content.RemoveAt(index);

        // Remove precedingXmlText if it's just newline and exterior trivia
        if (index > 0 && newContent[index - 1] is XmlTextSyntax)
        {
            newContent = newContent.RemoveAt(index - 1);
        }

        var newDocumentation = documentation.WithContent(newContent);
        var newTriviaList = memberNode.GetLeadingTrivia().Replace(documentationTrivia, SyntaxFactory.Trivia(newDocumentation));
        var newNode = memberNode.WithLeadingTrivia(newTriviaList);

        return document.WithSyntaxRoot(root.ReplaceNode(memberNode, newNode));
    }

    private static SyntaxTrivia GetDocumentationCommentTrivia(SyntaxNode node)
    {
        return node.GetLeadingTrivia()
            .FirstOrDefault(t => t.HasStructure && t.GetStructure() is DocumentationCommentTriviaSyntax);
    }

    private static XmlElementSyntax CreateXmlElement(string tagName, string? nameAttribute, string content, string indentation)
    {
        var xml = nameAttribute != null
            ? $"<{tagName} name=\"{nameAttribute}\">{content}</{tagName}>"
            : $"<{tagName}>{content}</{tagName}>";

        var trivia = SyntaxFactory.ParseLeadingTrivia("/// " + xml);
        var doc = (DocumentationCommentTriviaSyntax)trivia[0].GetStructure()!;
        var element = doc.Content.OfType<XmlElementSyntax>().First();
        return element.WithLeadingTrivia(SyntaxFactory.ParseLeadingTrivia($"\n{indentation}/// "));
    }
}
