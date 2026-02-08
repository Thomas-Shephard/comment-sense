using System.Xml.Linq;
using System.Xml;

namespace CommentSense.Core.Utilities;

internal static class DocumentationXmlExtensions
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

    public static bool HasValidDocumentation(string? xml)
    {
        return TryParseDocumentation(xml, out var element) && HasValidDocumentation(element);
    }

    public static bool HasValidDocumentation(XElement root)
    {
        return GetTargetElements(root)
            .Select(element => element.Name.LocalName)
            .Any(name => AutoValidTags.Contains(name) || ContentRequiredTags.Contains(name));
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
        var target = GetEffectiveTarget(root);

        if (tagName == null)
            return recursive ? target.Descendants() : target.Elements();

        return recursive ? target.Descendants(tagName) : target.Elements(tagName);
    }

    public static bool IsTopLevel(XElement root, XElement element, XElement? effectiveTarget = null)
    {
        return element.Parent != null && element.Parent == (effectiveTarget ?? GetEffectiveTarget(root));
    }

    public static XElement GetEffectiveTarget(XElement root)
    {
        return root.Name.LocalName == MemberTagName ? root : root.Element(MemberTagName) ?? root;
    }

    public static IEnumerable<string> GetElementAttributeValues(XElement root, string tagName, string attributeName, bool topLevelOnly = false)
    {
        var elements = GetTargetElements(root, tagName, recursive: !topLevelOnly);

        return elements
               .Select(d => d.Attribute(attributeName)?.Value)
               .Where(v => !string.IsNullOrWhiteSpace(v))
               .OfType<string>();
    }
}
