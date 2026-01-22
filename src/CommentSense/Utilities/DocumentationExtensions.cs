using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace CommentSense.Utilities;

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

    internal static bool HasValidDocumentation(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasValidDocumentation(element);
    }

    internal static bool HasValidDocumentation(XElement root)
    {
        foreach (var descendant in root.Descendants())
        {
            var name = descendant.Name.LocalName;

            if (AutoValidTags.Contains(name))
                return true;

            if (ContentRequiredTags.Contains(name) && (descendant.HasElements || !string.IsNullOrWhiteSpace(descendant.Value)))
                return true;
        }

        return false;
    }

    internal static bool TryParseDocumentation(string? xml, out XElement element)
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

    internal static bool HasAutoValidTag(XElement root)
    {
        return root.Descendants().Any(element => AutoValidTags.Contains(element.Name.LocalName));
    }

    internal static IEnumerable<string> GetParamNames(XElement root)
    {
        return root.Descendants("param")
            .Where(d => d.HasElements || !string.IsNullOrWhiteSpace(d.Value))
            .Select(d => d.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OfType<string>();
    }

    internal static IEnumerable<string> GetParamNames(string? xml)
    {
        if (TryParseDocumentation(xml, out var element))
            return GetParamNames(element);

        return [];
    }

    internal static IEnumerable<string> GetTypeParamNames(XElement root)
    {
        return root.Descendants("typeparam")
            .Where(d => d.HasElements || !string.IsNullOrWhiteSpace(d.Value))
            .Select(d => d.Attribute("name")?.Value)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .OfType<string>();
    }

    internal static bool HasReturnsTag(XElement root)
    {
        return root.Descendants("returns")
            .Any(d => d.HasElements || !string.IsNullOrWhiteSpace(d.Value));
    }
}
