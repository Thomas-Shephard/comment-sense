using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class DocumentationExtensions
{
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

            if (ContentRequiredTags.Contains(name) && HasContent(element))
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

    public static IEnumerable<string> GetParamNames(XElement root)
    {
        return GetElementAttributeValues(root, "param", "name");
    }

    public static IEnumerable<string> GetParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetParamNames(element);

        return [];
    }

    public static IEnumerable<string> GetTypeParamNames(XElement root)
    {
        return GetElementAttributeValues(root, "typeparam", "name");
    }

    public static IEnumerable<string> GetTypeParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetTypeParamNames(element);

        return [];
    }

    public static bool HasReturnsTag(XElement root)
    {
        return GetValidElements(root, "returns").Any();
    }

    public static bool HasReturnsTag(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasReturnsTag(element);
    }

    public static IEnumerable<string> GetExceptionCrefs(XElement root)
    {
        return GetElementAttributeValues(root, "exception", "cref");
    }

    public static IEnumerable<string> GetExceptionCrefs(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetExceptionCrefs(element);

        return [];
    }

    private static IEnumerable<XElement> GetValidElements(XElement root, string tagName)
    {
        return GetTargetElements(root, tagName).Where(HasContent);
    }

    private static IEnumerable<XElement> GetTargetElements(XElement root, string? tagName = null)
    {
        var target = root.Name.LocalName == "member" ? root : root.Element("member") ?? root;
        return tagName == null ? target.Elements() : target.Elements(tagName);
    }

    private static IEnumerable<string> GetElementAttributeValues(XElement root, string tagName, string attributeName)
    {
        return GetValidElements(root, tagName)
               .Select(d => d.Attribute(attributeName)?.Value)
               .Where(v => !string.IsNullOrWhiteSpace(v))
               .OfType<string>();
    }

    private static bool HasContent(XElement element)
    {
        return element.HasElements || !string.IsNullOrWhiteSpace(element.Value);
    }
}
