using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationExtensions
{
    private const string MemberTagName = "member";

    private static readonly HashSet<string> AutoValidTags = [
        DocumentationTags.InheritDoc, DocumentationTags.Include
    ];
    private static readonly HashSet<string> ContentRequiredTags = [
        DocumentationTags.Summary, DocumentationTags.Remarks, DocumentationTags.Returns, DocumentationTags.Value,
        DocumentationTags.Param, DocumentationTags.TypeParam, DocumentationTags.Exception, DocumentationTags.Example,
        DocumentationTags.SeeAlso, DocumentationTags.Permission
    ];

    public static bool HasValidDocumentation(this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        return HasValidDocumentation(symbol.GetDocumentationCommentXml());
    }

    public static bool HasValidDocumentation(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasValidDocumentation(element);
    }

    public static bool HasValidDocumentation(XElement root)
    {
        foreach (var element in GetTargetElements(root))
        {
            var name = element.Name.LocalName;

            if (AutoValidTags.Contains(name))
                return true;

            if (ContentRequiredTags.Contains(name))
                return true;
        }

        return false;
    }

    public static bool TryParseDocumentation(string? xml, out XElement element)
    {
        if (string.IsNullOrWhiteSpace(xml))
        {
            element = new XElement("root");
            return false;
        }

        try
        {
            element = XElement.Parse($"<root>{xml}</root>");
            return true;
        }
        catch (XmlException)
        {
            element = new XElement("root");
            return false;
        }
    }

    public static bool HasAutoValidTag(XElement root)
    {
        return GetTargetElements(root).Any(element => AutoValidTags.Contains(element.Name.LocalName));
    }

    public static bool HasInheritDoc(XElement root)
    {
        return root.Descendants(DocumentationTags.InheritDoc).Any();
    }

    public static bool HasInheritDocWithCref(XElement root)
    {
        return root.Descendants(DocumentationTags.InheritDoc).Any(e => e.Attribute(DocumentationAttributes.Cref) != null);
    }

    public static IEnumerable<string> GetNames(XElement root, string tagName, string attributeName = DocumentationAttributes.Name, bool topLevelOnly = true)
    {
        return GetElementAttributeValues(root, tagName, attributeName, topLevelOnly);
    }

    public static IEnumerable<string> GetParamNames(XElement root)
    {
        return GetNames(root, DocumentationTags.Param, topLevelOnly: true);
    }

    public static IEnumerable<string> GetParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetParamNames(element);

        return [];
    }

    public static IEnumerable<string> GetTypeParamNames(XElement root)
    {
        return GetNames(root, DocumentationTags.TypeParam, topLevelOnly: true);
    }

    public static IEnumerable<string> GetTypeParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetTypeParamNames(element);

        return [];
    }

    public static bool HasReturnsTag(XElement root)
    {
        return GetTargetElements(root, DocumentationTags.Returns, recursive: false).Any();
    }

    public static bool HasReturnsTag(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasReturnsTag(element);
    }

    public static bool HasValueTag(XElement root)
    {
        return GetTargetElements(root, DocumentationTags.Value, recursive: false).Any();
    }

    public static IEnumerable<string> GetExceptionCrefs(XElement root)
    {
        return GetNames(root, DocumentationTags.Exception, DocumentationAttributes.Cref, topLevelOnly: true);
    }

    public static IEnumerable<string> GetExceptionCrefs(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetExceptionCrefs(element);

        return [];
    }

    public static IEnumerable<XElement> GetTargetElements(XElement root, string? tagName = null, bool recursive = false)
    {
        var target = root.Name.LocalName == MemberTagName ? root : root.Element(MemberTagName) ?? root;

        if (tagName == null)
            return recursive ? target.Descendants() : target.Elements();

        return recursive ? target.Descendants(tagName) : target.Elements(tagName);
    }

    public static IEnumerable<(XElement Element, Location Location)> GetTargetElementsWithLocations(this ISymbol symbol, XElement xml, string tagName, bool topLevelOnly = false)
    {
        var elements = GetTargetElements(xml, tagName, recursive: !topLevelOnly).ToList();
        var locations = symbol.GetDocumentationLocations(tagName, topLevelOnly: topLevelOnly);

        for (int i = 0; i < elements.Count; i++)
        {
            yield return (elements[i], locations.GetLocationOrDefault(i, symbol));
        }
    }

    public static IEnumerable<string> GetElementAttributeValues(XElement root, string tagName, string attributeName, bool topLevelOnly = false)
    {
        var elements = GetTargetElements(root, tagName, recursive: !topLevelOnly);

        return elements
               .Select(d => d.Attribute(attributeName)?.Value)
               .Where(v => !string.IsNullOrWhiteSpace(v))
               .OfType<string>();
    }

    public static string GetTagName(this XmlNodeSyntax xmlNode)
    {
        return xmlNode switch
        {
            XmlElementSyntax element           => element.StartTag.Name.LocalName.ValueText,
            XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText,
            _                                  => string.Empty
        };
    }

    public static string? GetNameAttribute(this XmlNodeSyntax xmlNode)
    {
        var attributes = xmlNode switch
        {
            XmlElementSyntax element           => element.StartTag.Attributes,
            XmlEmptyElementSyntax emptyElement => emptyElement.Attributes,
            _                                  => default
        };

        foreach (var attribute in attributes)
        {
            if (attribute is XmlNameAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } nameAttr)
                return nameAttr.Identifier.Identifier.ValueText;

            if (attribute is XmlTextAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } textAttr)
            {
                return string.Concat(textAttr.TextTokens.Select(t => t.ValueText));
            }
        }

        return null;
    }

    public static XmlTextSyntax? GetAssociatedWhitespaceToRemove(this XmlNodeSyntax xmlNode)
    {
        var content = xmlNode.Parent switch
        {
            DocumentationCommentTriviaSyntax doc => doc.Content,
            XmlElementSyntax element             => element.Content,
            _                                    => default
        };

        if (content == default)
            return null;

        return GetAssociatedWhitespaceToRemove(xmlNode, content);
    }

    public static XmlTextSyntax? GetAssociatedWhitespaceToRemove(XmlNodeSyntax xmlNode, SyntaxList<XmlNodeSyntax> content)
    {
        var index = content.IndexOf(xmlNode);
        if (index == -1)
            return null;

        var trailing = index + 1 < content.Count ? content[index + 1] as XmlTextSyntax : null;
        var leading = index > 0 ? content[index - 1] as XmlTextSyntax : null;

        bool isAtStart = index == 0 || (index == 1 && leading != null && !leading.ToString().Contains("\n") && leading.IsPureWhitespaceOrPrefix());
        if (isAtStart && trailing.IsPureWhitespaceOrPrefix())
            return trailing;

        if (leading.IsPureWhitespaceOrPrefix())
            return leading;

        if (index + 1 < content.Count - 1 && trailing.IsPureWhitespaceOrPrefix())
            return trailing;

        return null;
    }

    public static bool IsPureWhitespaceOrPrefix(this XmlTextSyntax? xmlText)
    {
        if (xmlText == null)
            return false;

        var text = xmlText.ToString();
        var allValidChars = text.All(c => char.IsWhiteSpace(c) || c == '/');
        return string.IsNullOrEmpty(text) || (allValidChars && (text.Contains("///") || text.All(char.IsWhiteSpace)));
    }

    public static (SyntaxNode? Parent, SyntaxList<XmlNodeSyntax> Content) GetParentContent(this XmlNodeSyntax xmlNode)
    {
        if (xmlNode.Parent is DocumentationCommentTriviaSyntax d)
            return (d, d.Content);

        if (xmlNode.Parent is XmlElementSyntax e)
            return (e, e.Content);

        return (null, default);
    }

    public static MemberDeclarationSyntax? GetMemberDeclaration(this SyntaxNode? node)
    {
        if (node == null)
            return null;

        var docTrivia = node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        var targetNode = docTrivia != null ? docTrivia.ParentTrivia.Token.Parent : node;
        return targetNode?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
    }

    public static Location GetPrimaryLocation(this System.Collections.Immutable.ImmutableArray<Location> locations)
    {
        if (locations.Length == 0)
            return Location.None;

        return locations[0];
    }

    public static Location GetLocationOrDefault(this System.Collections.Immutable.ImmutableArray<Location> locations, int index, ISymbol symbol)
    {
        return index >= 0 && index < locations.Length
            ? locations[index]
            : symbol.Locations.GetPrimaryLocation();
    }

    public static Location GetDocumentationLocation(this ISymbol symbol, string tagName, string? attributeValue = null, int occurrence = 0, string attributeName = DocumentationAttributes.Name, bool topLevelOnly = true)
    {
        return symbol.GetDocumentationLocations(tagName, attributeValue, attributeName, topLevelOnly).GetLocationOrDefault(occurrence, symbol);
    }

    public static System.Collections.Immutable.ImmutableArray<Location> GetDocumentationLocations(this ISymbol symbol, string tagName, string? attributeValue = null, string attributeName = DocumentationAttributes.Name, bool topLevelOnly = true)
    {
        var builder = System.Collections.Immutable.ImmutableArray.CreateBuilder<Location>();

        var docTrivias = symbol.DeclaringSyntaxReferences
                               .Select(r => r.GetSyntax())
                               .Select(GetDocumentationCommentTrivia)
                               .OfType<DocumentationCommentTriviaSyntax>();

        foreach (var docTrivia in docTrivias)
        {
            GetDocumentationLocationsInternal(docTrivia, tagName, attributeValue, attributeName, builder, topLevelOnly);
        }

        return builder.ToImmutable();
    }

    private static DocumentationCommentTriviaSyntax? GetDocumentationCommentTrivia(SyntaxNode syntax)
    {
        // Documentation trivia might be on the member declaration rather than the specific declarator (e.g. for fields/events)
        var current = syntax;
        while (current != null)
        {
            var docTrivia = current.GetLeadingTrivia()
                .Select(t => t.GetStructure())
                .OfType<DocumentationCommentTriviaSyntax>()
                .FirstOrDefault();

            if (docTrivia != null)
                return docTrivia;

            if (current is MemberDeclarationSyntax or CompilationUnitSyntax)
                break;

            current = current.Parent;
        }

        return null;
    }

    private static void GetDocumentationLocationsInternal(DocumentationCommentTriviaSyntax docTrivia, string tagName, string? attributeValue, string attributeName, System.Collections.Immutable.ImmutableArray<Location>.Builder builder, bool topLevelOnly)
    {
        var nodes = topLevelOnly ? docTrivia.Content : docTrivia.DescendantNodes();
        foreach (var node in nodes)
        {
            bool matches = node switch
            {
                XmlElementSyntax element => element.StartTag.Name.LocalName.ValueText == tagName && (attributeValue == null || HasAttribute(element, attributeName, attributeValue)),
                XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText == tagName && (attributeValue == null || HasAttribute(emptyElement, attributeName, attributeValue)),
                _ => false
            };

            if (matches)
            {
                builder.Add(node.GetLocation());
            }
        }
    }

    private static bool HasAttribute(XmlElementSyntax element, string attributeName, string value)
    {
        return element.StartTag.Attributes.Any(a => MatchAttribute(a, attributeName, value));
    }

    private static bool HasAttribute(XmlEmptyElementSyntax element, string attributeName, string value)
    {
        return element.Attributes.Any(a => MatchAttribute(a, attributeName, value));
    }

    private static bool MatchAttribute(XmlAttributeSyntax attribute, string name, string value)
    {
        return attribute switch
        {
            XmlNameAttributeSyntax nameAttr => nameAttr.Name.LocalName.ValueText == name && nameAttr.Identifier.Identifier.ValueText == value,
            XmlCrefAttributeSyntax crefAttr => crefAttr.Name.LocalName.ValueText == name && (crefAttr.Cref.ToString() == value || $"T:{crefAttr.Cref}" == value),
            XmlTextAttributeSyntax textAttr => textAttr.Name.LocalName.ValueText == name && string.Concat(textAttr.TextTokens.Select(t => t.ValueText)) == value,
            _                               => false
        };
    }
}
