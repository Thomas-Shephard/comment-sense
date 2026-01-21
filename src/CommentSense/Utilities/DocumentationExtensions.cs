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
        if (string.IsNullOrWhiteSpace(xml))
            return false;

        try
        {
            var element = XElement.Parse($"<root>{xml}</root>");

            foreach (var descendant in element.Descendants())
            {
                var name = descendant.Name.LocalName;

                if (AutoValidTags.Contains(name))
                    return true;

                if (ContentRequiredTags.Contains(name) && (descendant.HasElements || !string.IsNullOrWhiteSpace(descendant.Value)))
                    return true;
            }

            return false;
        }
        catch (XmlException)
        {
            return false;
        }
    }
}
