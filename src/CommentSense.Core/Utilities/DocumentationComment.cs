using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal sealed class DocumentationComment
{
    private static readonly HashSet<string> AutoValidTags =
    [
        DocumentationTags.InheritDoc, DocumentationTags.Include
    ];

    private static readonly HashSet<string> ContentRequiredTags =
    [
        DocumentationTags.Summary, DocumentationTags.Remarks, DocumentationTags.Returns, DocumentationTags.Value,
        DocumentationTags.Param, DocumentationTags.TypeParam, DocumentationTags.Exception, DocumentationTags.Example,
        DocumentationTags.SeeAlso, DocumentationTags.Permission
    ];

    private DocumentationComment(IReadOnlyList<DocumentationCommentTriviaSyntax> trivias)
    {
        Trivias = trivias;
    }

    private IReadOnlyList<DocumentationCommentTriviaSyntax> Trivias { get; }

    public bool IsMalformedFor(ISymbol symbol, CancellationToken cancellationToken = default)
    {
        if (!Trivias.Any(static trivia => trivia.ContainsDiagnostics))
            return false;

        return !DocumentationXmlExtensions.TryParseDocumentation(symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken), out _);
    }

    public IEnumerable<XmlNodeSyntax> GetElements(string? tagName = null, bool recursive = false)
    {
        foreach (var trivia in Trivias)
        {
            var nodes = recursive
                ? trivia.DescendantNodes()
                : trivia.Content;

            foreach (var node in nodes)
            {
                if (node is not XmlNodeSyntax xmlNode)
                    continue;

                if (!xmlNode.IsElement())
                    continue;

                if (tagName == null || xmlNode.GetTagName() == tagName)
                    yield return xmlNode;
            }
        }
    }

    public bool IsTopLevel(XmlNodeSyntax node)
    {
        return node.Parent is DocumentationCommentTriviaSyntax trivia && Trivias.Contains(trivia);
    }

    public bool HasValidDocumentation()
    {
        return GetElements().Any(element =>
        {
            var tagName = element.GetTagName();
            return AutoValidTags.Contains(tagName) || ContentRequiredTags.Contains(tagName);
        });
    }

    public bool HasReturnsTag()
    {
        return GetElements(DocumentationTags.Returns, recursive: false).Any();
    }

    public bool HasValueTag()
    {
        return GetElements(DocumentationTags.Value, recursive: false).Any();
    }

    public IReadOnlyList<string> GetAttributeValues(string tagName, string attributeName, bool topLevelOnly = false)
    {
        var values = new List<string>();
        var elements = GetElements(tagName, recursive: !topLevelOnly);

        foreach (var element in elements)
        {
            var value = element.GetAttributeValue(attributeName);
            if (value is not null && !string.IsNullOrWhiteSpace(value))
                values.Add(value);
        }

        return values;
    }

    public static DocumentationComment? FromSymbol(ISymbol symbol, CancellationToken cancellationToken = default)
    {
        List<DocumentationCommentTriviaSyntax>? trivias = null;

        foreach (var syntaxReference in GetDeclaringSyntaxReferences(symbol))
        {
            if (syntaxReference.SyntaxTree.Options.DocumentationMode == DocumentationMode.None)
                continue;

            var syntax = syntaxReference.GetSyntax(cancellationToken);
            var docTrivia = DocumentationLocationExtensions.GetDocumentationCommentTrivia(syntax);
            if (docTrivia != null)
            {
                trivias ??= [];
                trivias.Add(docTrivia);
            }
        }

        if (trivias is not { Count: > 0 })
            return null;

        return new DocumentationComment(trivias);
    }

    public static IEnumerable<SyntaxReference> GetDeclaringSyntaxReferences(ISymbol symbol)
    {
        foreach (var currentSymbol in GetSymbolsToInspect(symbol))
        {
            foreach (var syntaxReference in currentSymbol.DeclaringSyntaxReferences)
            {
                yield return syntaxReference;
            }
        }
    }

    private static IEnumerable<ISymbol> GetSymbolsToInspect(ISymbol symbol)
    {
        symbol = symbol switch
        {
            IMethodSymbol { PartialDefinitionPart: { } definition } => definition,
            IPropertySymbol { PartialDefinitionPart: { } definition } => definition,
            IEventSymbol { PartialDefinitionPart: { } definition } => definition,
            _ => symbol
        };

        yield return symbol;

        ISymbol? implementation = symbol switch
        {
            IMethodSymbol method => method.PartialImplementationPart,
            IPropertySymbol property => property.PartialImplementationPart,
            IEventSymbol @event => @event.PartialImplementationPart,
            _ => null
        };
        if (implementation != null)
            yield return implementation;
    }
}
