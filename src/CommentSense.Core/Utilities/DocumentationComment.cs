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
        TriviaSet = [.. trivias];
    }

    private IReadOnlyList<DocumentationCommentTriviaSyntax> Trivias { get; }
    private HashSet<DocumentationCommentTriviaSyntax> TriviaSet { get; }

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
        return node.Parent is DocumentationCommentTriviaSyntax trivia && TriviaSet.Contains(trivia);
    }

    public bool HasValidDocumentation()
    {
        return GetElements().Any(element =>
        {
            var tagName = element.GetTagName();
            return AutoValidTags.Contains(tagName) || ContentRequiredTags.Contains(tagName);
        });
    }

    public bool HasAutoValidTag()
    {
        return GetElements().Any(element => AutoValidTags.Contains(element.GetTagName()));
    }

    public bool HasInheritDoc()
    {
        return GetElements(DocumentationTags.InheritDoc, recursive: true).Any();
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
        if (symbol is not IMethodSymbol method)
        {
            yield return symbol;
            yield break;
        }

        foreach (var methodSymbol in GetMethodSymbolsToInspect(method))
            yield return methodSymbol;
    }

    private static IEnumerable<IMethodSymbol> GetMethodSymbolsToInspect(IMethodSymbol method)
    {
        var directMethods = GetDirectMethodSymbols(method);
        if (directMethods.Count > 1)
        {
            foreach (var symbol in OrderMethodSymbols(directMethods))
                yield return symbol;

            yield break;
        }

        yield return method;
    }

    private static List<IMethodSymbol> GetDirectMethodSymbols(IMethodSymbol method)
    {
        var methods = new List<IMethodSymbol>(capacity: 3);
        AddIfMissing(methods, method.PartialDefinitionPart);
        AddIfMissing(methods, method);
        AddIfMissing(methods, method.PartialImplementationPart);
        return methods;
    }

    private static void AddIfMissing(List<IMethodSymbol> methods, IMethodSymbol? candidate)
    {
        if (candidate == null)
            return;

        if (methods.Any(existing => SymbolEqualityComparer.Default.Equals(existing, candidate)))
            return;

        methods.Add(candidate);
    }

    private static List<IMethodSymbol> OrderMethodSymbols(List<IMethodSymbol> methods)
    {
        if (methods.Count <= 1)
            return methods;

        var orderedMethods = methods
            .Select((symbol, index) => (Symbol: symbol, Index: index))
            .ToList();

        orderedMethods.Sort(static (left, right) =>
        {
            int orderComparison = GetMethodDeclarationOrder(left.Symbol).CompareTo(GetMethodDeclarationOrder(right.Symbol));
            return orderComparison != 0
                ? orderComparison
                : left.Index.CompareTo(right.Index);
        });

        return [.. orderedMethods.Select(static entry => entry.Symbol)];
    }

    private static int GetMethodDeclarationOrder(IMethodSymbol method)
    {
        return method.PartialImplementationPart != null ? 0 : 1;
    }
}
