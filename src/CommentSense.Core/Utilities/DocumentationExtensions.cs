using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationExtensions
{
    private const string MemberTagName = "member";

    private static readonly HashSet<string> AutoValidTags = [
        "inheritdoc", "include"
    ];
    private static readonly HashSet<string> ContentRequiredTags = [
        "summary", "remarks", "returns", "value", "param", "typeparam", "exception", "example", "seealso", "permission"
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
        return root.Descendants("inheritdoc").Any();
    }

    public static bool HasInheritDocWithCref(XElement root)
    {
        return root.Descendants("inheritdoc").Any(e => e.Attribute("cref") != null);
    }

    public static IEnumerable<string> GetParamNames(XElement root)
    {
        return GetElementAttributeValues(root, "param", "name", topLevelOnly: true);
    }

    public static IEnumerable<string> GetParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetParamNames(element);

        return [];
    }

    public static IEnumerable<string> GetTypeParamNames(XElement root)
    {
        return GetElementAttributeValues(root, "typeparam", "name", topLevelOnly: true);
    }

    public static IEnumerable<string> GetTypeParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetTypeParamNames(element);

        return [];
    }

    public static bool HasReturnsTag(XElement root)
    {
        return GetTopLevelElements(root, "returns").Any();
    }

    public static bool HasReturnsTag(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasReturnsTag(element);
    }

    public static bool HasValueTag(XElement root)
    {
        return GetTopLevelElements(root, "value").Any();
    }

    public static IEnumerable<string> GetExceptionCrefs(XElement root)
    {
        return GetElementAttributeValues(root, "exception", "cref", topLevelOnly: true);
    }

    public static IEnumerable<string> GetExceptionCrefs(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetExceptionCrefs(element);

        return [];
    }

    public static IEnumerable<XElement> GetTargetElements(XElement root, string? tagName = null)
    {
        var target = root.Name.LocalName == MemberTagName ? root : root.Element(MemberTagName) ?? root;

        if (tagName == null)
            return target.Elements();

        if (tagName is "param" or "typeparam" or "returns" or "value")
            return target.Descendants(tagName);

        return target.Elements(tagName);
    }

    public static IEnumerable<XElement> GetTopLevelElements(XElement root, string tagName)
    {
        var target = root.Name.LocalName == MemberTagName ? root : root.Element(MemberTagName) ?? root;
        return target.Elements(tagName);
    }

    public static IEnumerable<string> GetElementAttributeValues(XElement root, string tagName, string attributeName, bool topLevelOnly = false)
    {
        var elements = topLevelOnly
            ? GetTopLevelElements(root, tagName)
            : GetTargetElements(root, tagName);

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

        return attributes.OfType<XmlNameAttributeSyntax>().FirstOrDefault(a => a.Name.LocalName.ValueText == "name")?.Identifier.Identifier.ValueText;
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
}
