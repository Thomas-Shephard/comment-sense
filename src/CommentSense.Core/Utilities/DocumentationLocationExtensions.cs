using System.Collections.Immutable;
using System.Xml.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationLocationExtensions
{
    public static IEnumerable<(XElement Element, Location Location)> GetTargetElementsWithLocations(this ISymbol symbol, XElement xml, string tagName, bool topLevelOnly = true)
    {
        var locations = symbol.GetDocumentationLocations(tagName, topLevelOnly: topLevelOnly);
        var elements = DocumentationXmlExtensions.GetTargetElements(xml, tagName, recursive: !topLevelOnly).ToList();

        for (int i = 0; i < Math.Min(locations.Length, elements.Count); i++)
        {
            yield return (elements[i], locations[i]);
        }
    }

    public static IEnumerable<Location> GetTargetElementsWithLocations(this ISymbol symbol, string tagName)
    {
        var locations = symbol.GetDocumentationLocations(tagName);
        if (locations.IsDefaultOrEmpty)
            yield break;

        foreach (var location in locations)
        {
            yield return location;
        }
    }

    public static Location GetPrimaryLocation(this ImmutableArray<Location> locations)
    {
        if (locations.IsDefaultOrEmpty)
            return Location.None;

        return locations[0];
    }

    public static Location GetLocationOrDefault(this ImmutableArray<Location> locations, int occurrence, ISymbol symbol)
    {
        if (locations.IsDefaultOrEmpty || occurrence < 0 || occurrence >= locations.Length)
            return GetSymbolLocation(symbol);

        return locations[occurrence];
    }

    public static Location GetSymbolLocation(ISymbol symbol)
    {
        var locations = symbol.Locations;
        if (locations.Length > 0)
            return locations[0];

        return Location.None;
    }

    public static Location GetDocumentationLocation(this ISymbol symbol, string tagName, string? attributeValue = null, int occurrence = 0, string attributeName = DocumentationAttributes.Name, bool topLevelOnly = true)
    {
        return symbol.GetDocumentationLocations(tagName, attributeValue, attributeName, topLevelOnly).GetLocationOrDefault(occurrence, symbol);
    }

    public static ImmutableArray<Location> GetDocumentationLocations(this ISymbol symbol, string tagName, string? attributeValue = null, string attributeName = DocumentationAttributes.Name, bool topLevelOnly = true)
    {
        var builder = ImmutableArray.CreateBuilder<Location>();

        var trivias = symbol.DeclaringSyntaxReferences
            .Select(r => GetDocumentationCommentTrivia(r.GetSyntax()))
            .OfType<DocumentationCommentTriviaSyntax>();

        foreach (var docTrivia in trivias)
        {
            GetDocumentationLocationsInternal(docTrivia, tagName, attributeValue, attributeName, builder, topLevelOnly);
        }

        return builder.ToImmutable();
    }

    public static DocumentationCommentTriviaSyntax? GetDocumentationCommentTrivia(SyntaxNode? syntax)
    {
        SyntaxNode? current = syntax;
        while (current != null)
        {
            if (current.HasStructuredTrivia)
            {
                var trivia = current.GetLeadingTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) || t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
                if (trivia.GetStructure() is DocumentationCommentTriviaSyntax docTrivia)
                    return docTrivia;
            }

            if (current is MemberDeclarationSyntax or CompilationUnitSyntax)
                return null;

            current = current.Parent;
        }

        return null;
    }

    public static void GetDocumentationLocationsInternal(DocumentationCommentTriviaSyntax docTrivia, string tagName, string? attributeValue, string attributeName, ImmutableArray<Location>.Builder builder, bool topLevelOnly)
    {
        var nodes = topLevelOnly
            ? docTrivia.Content
            : docTrivia.DescendantNodes();

        builder.AddRange(nodes
            .Where(node => IsMatch(node, tagName, attributeValue, attributeName))
            .Select(node => node.GetLocation()));
    }

    private static bool IsMatch(SyntaxNode node, string tagName, string? attributeValue, string attributeName)
    {
        return node switch
        {
            XmlElementSyntax element when element.StartTag.Name.LocalName.ValueText == tagName =>
                attributeValue == null || HasAttribute(element, attributeName, attributeValue),

            XmlEmptyElementSyntax emptyElement when emptyElement.Name.LocalName.ValueText == tagName =>
                attributeValue == null || HasAttribute(emptyElement, attributeName, attributeValue),

            _ => false
        };
    }

    public static bool HasAttribute(XmlElementSyntax element, string name, string value)
    {
        return element.StartTag.Attributes.Any(attr => MatchAttribute(attr, name, value));
    }

    public static bool HasAttribute(XmlEmptyElementSyntax element, string name, string value)
    {
        return element.Attributes.Any(attr => MatchAttribute(attr, name, value));
    }

    public static bool MatchAttribute(XmlAttributeSyntax attribute, string name, string value)
    {
        switch (attribute)
        {
            case XmlNameAttributeSyntax nameAttr when nameAttr.Name.LocalName.ValueText == name:
                return nameAttr.Identifier.Identifier.ValueText == value;
            case XmlCrefAttributeSyntax crefAttr when crefAttr.Name.LocalName.ValueText == name:
            {
                string crefStr = crefAttr.Cref.ToString();
                if (crefStr == value)
                    return true;

                return "T:" + crefStr == value;
            }
            case XmlTextAttributeSyntax textAttr when textAttr.Name.LocalName.ValueText == name:
                return string.Concat(textAttr.TextTokens.Select(t => t.ValueText)) == value;
            default:
                return false;
        }
    }
}
