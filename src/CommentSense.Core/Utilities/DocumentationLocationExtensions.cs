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
        var elements = DocumentationXmlExtensions.GetTargetElements(xml, tagName, recursive: !topLevelOnly);

        var locationIndex = 0;
        foreach (var element in elements)
        {
            if (locationIndex >= locations.Length)
                break;

            yield return (element, locations[locationIndex++]);
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

        foreach (var r in symbol.DeclaringSyntaxReferences)
        {
            var docTrivia = GetDocumentationCommentTrivia(r.GetSyntax());
            if (docTrivia != null)
            {
                GetDocumentationLocationsInternal(docTrivia, tagName, attributeValue, attributeName, builder, topLevelOnly);
            }
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
                var docTrivia = FindDocumentationTrivia(current.GetLeadingTrivia());
                if (docTrivia != null)
                    return docTrivia;
            }

            if (current is MemberDeclarationSyntax or CompilationUnitSyntax)
                return null;

            current = current.Parent;
        }

        return null;
    }

    private static DocumentationCommentTriviaSyntax? FindDocumentationTrivia(SyntaxTriviaList triviaList)
    {
        for (int i = triviaList.Count - 1; i >= 0; i--)
        {
            var trivia = triviaList[i];
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                continue;

            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax docTrivia)
                return docTrivia;
        }

        return null;
    }

    public static void GetDocumentationLocationsInternal(DocumentationCommentTriviaSyntax docTrivia, string tagName, string? attributeValue, string attributeName, ImmutableArray<Location>.Builder builder, bool topLevelOnly)
    {
        var nodes = topLevelOnly
            ? docTrivia.Content
            : docTrivia.DescendantNodes();

        foreach (var node in nodes)
        {
            if (IsMatch(node, tagName, attributeValue, attributeName))
                builder.Add(node.GetLocation());
        }
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
        foreach (var attr in element.StartTag.Attributes)
        {
            if (MatchAttribute(attr, name, value))
                return true;
        }

        return false;
    }

    public static bool HasAttribute(XmlEmptyElementSyntax element, string name, string value)
    {
        foreach (var attr in element.Attributes)
        {
            if (MatchAttribute(attr, name, value))
                return true;
        }

        return false;
    }

    public static bool MatchAttribute(XmlAttributeSyntax attribute, string name, string value)
    {
        var valueSpan = value.AsSpan();
        return attribute switch
        {
            XmlNameAttributeSyntax nameAttr => MatchNameAttribute(nameAttr, name, valueSpan),
            XmlCrefAttributeSyntax crefAttr => MatchCrefAttribute(crefAttr, name, valueSpan),
            XmlTextAttributeSyntax textAttr => MatchTextAttribute(textAttr, name, valueSpan),
            _ => false
        };
    }

    private static bool MatchNameAttribute(XmlNameAttributeSyntax nameAttr, string name, ReadOnlySpan<char> valueSpan)
    {
        return nameAttr.Name.LocalName.ValueText == name &&
               nameAttr.Identifier.Identifier.ValueText.AsSpan().Equals(valueSpan, StringComparison.Ordinal);
    }

    private static bool MatchCrefAttribute(XmlCrefAttributeSyntax crefAttr, string name, ReadOnlySpan<char> valueSpan)
    {
        if (crefAttr.Name.LocalName.ValueText != name)
            return false;

        string crefStr = crefAttr.Cref.ToString();
        if (crefStr.AsSpan().Equals(valueSpan, StringComparison.Ordinal))
            return true;

        return valueSpan.Length >= 2 && valueSpan[0] == 'T' && valueSpan[1] == ':' &&
               crefStr.AsSpan().Equals(valueSpan.Slice(2), StringComparison.Ordinal);
    }

    private static bool MatchTextAttribute(XmlTextAttributeSyntax textAttr, string name, ReadOnlySpan<char> valueSpan)
    {
        if (textAttr.Name.LocalName.ValueText != name)
            return false;

        if (textAttr.TextTokens.Count == 0)
            return valueSpan.IsEmpty;

        if (textAttr.TextTokens.Count == 1)
            return textAttr.TextTokens[0].ValueText.AsSpan().Equals(valueSpan, StringComparison.Ordinal);

        int totalLength = 0;
        foreach (var token in textAttr.TextTokens)
        {
            totalLength += token.ValueText.Length;
        }

        if (totalLength != valueSpan.Length)
            return false;

        int offset = 0;
        foreach (var token in textAttr.TextTokens)
        {
            var tokenText = token.ValueText;
            if (!valueSpan.Slice(offset, tokenText.Length).Equals(tokenText.AsSpan(), StringComparison.Ordinal))
                return false;

            offset += tokenText.Length;
        }

        return true;
    }
}
