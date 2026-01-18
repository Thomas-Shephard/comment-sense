using System.Xml;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;

namespace CommentSense.Utilities;

internal static class DocumentationExtensions
{
    public static bool HasValidDocumentation(this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
        {
            return false;
        }

        try
        {
            var element = XElement.Parse(xml);
            
            // Check for <inheritdoc />
            if (element.Descendants("inheritdoc").Any())
            {
                return true;
            }

            // Check if any of the major documentation tags have non-whitespace content.
            var tagsToCheck = new[] { "summary", "remarks", "returns", "value" };
            if (tagsToCheck.Any(tag =>
                {
                    var tagElement = element.Element(tag);
                    return tagElement is not null && !string.IsNullOrWhiteSpace(tagElement.Value);
                }))
            {
                return true;
            }

            // Check for <param> tags with content.
            if (element.Elements("param").Any(p => !string.IsNullOrWhiteSpace(p.Value)))
            {
                return true;
            }

            // Check for <exception> tags with content.
            if (element.Elements("exception").Any(e => !string.IsNullOrWhiteSpace(e.Value)))
            {
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
