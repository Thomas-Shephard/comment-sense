using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace CommentSense.Core.Utilities;

internal static class DocumentationSyntaxExtensions
{
    private static readonly Dictionary<SyntaxKind, string> PredefinedCrefAliases = new()
    {
        [SyntaxKind.BoolKeyword] = "System.Boolean",
        [SyntaxKind.ByteKeyword] = "System.Byte",
        [SyntaxKind.SByteKeyword] = "System.SByte",
        [SyntaxKind.ShortKeyword] = "System.Int16",
        [SyntaxKind.UShortKeyword] = "System.UInt16",
        [SyntaxKind.IntKeyword] = "System.Int32",
        [SyntaxKind.UIntKeyword] = "System.UInt32",
        [SyntaxKind.LongKeyword] = "System.Int64",
        [SyntaxKind.ULongKeyword] = "System.UInt64",
        [SyntaxKind.FloatKeyword] = "System.Single",
        [SyntaxKind.DoubleKeyword] = "System.Double",
        [SyntaxKind.DecimalKeyword] = "System.Decimal",
        [SyntaxKind.CharKeyword] = "System.Char",
        [SyntaxKind.StringKeyword] = "System.String",
        [SyntaxKind.ObjectKeyword] = "System.Object",
        [SyntaxKind.VoidKeyword] = "System.Void",
    };

    public static bool IsElement(this XmlNodeSyntax xmlNode) => xmlNode is XmlElementSyntax or XmlEmptyElementSyntax;

    public static string GetTagName(this XmlNodeSyntax xmlNode)
    {
        return xmlNode switch
        {
            XmlElementSyntax element => element.StartTag.Name.LocalName.ValueText,
            XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText,
            _ => string.Empty
        };
    }

    public static string? GetNameAttribute(this XmlNodeSyntax xmlNode)
    {
        var attributes = xmlNode switch
        {
            XmlElementSyntax element => element.StartTag.Attributes,
            XmlEmptyElementSyntax emptyElement => emptyElement.Attributes,
            _ => default
        };

        foreach (var attribute in attributes)
        {
            if (attribute is XmlNameAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } nameAttr)
                return nameAttr.Identifier.Identifier.ValueText;

            if (attribute is XmlTextAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } textAttr)
                return GetTextAttributeValue(textAttr);
        }

        return null;
    }

    public static string? GetAttributeValue(this XmlNodeSyntax xmlNode, string attributeName)
    {
        var attributes = xmlNode switch
        {
            XmlElementSyntax element => element.StartTag.Attributes,
            XmlEmptyElementSyntax emptyElement => emptyElement.Attributes,
            _ => default
        };

        foreach (var attribute in attributes)
        {
            switch (attribute)
            {
                case XmlNameAttributeSyntax nameAttr when nameAttr.Name.LocalName.ValueText == attributeName:
                    return nameAttr.Identifier.Identifier.ValueText;
                case XmlCrefAttributeSyntax crefAttr when crefAttr.Name.LocalName.ValueText == attributeName:
                    return GetCrefAttributeValue(crefAttr.Cref);
                case XmlTextAttributeSyntax textAttr when textAttr.Name.LocalName.ValueText == attributeName:
                    return GetTextAttributeValue(textAttr);
            }
        }

        return null;
    }

    public static bool HasChildElements(this XmlNodeSyntax xmlNode)
    {
        return xmlNode is XmlElementSyntax element && element.Content.Any(static child => child is XmlElementSyntax or XmlEmptyElementSyntax);
    }

    public static string GetInnerText(this XmlNodeSyntax xmlNode)
    {
        return xmlNode switch
        {
            XmlElementSyntax element => GetInnerText(element.Content),
            XmlTextSyntax text => GetTokenText(text.TextTokens),
            XmlCDataSectionSyntax cdata => GetTokenText(cdata.TextTokens),
            _ => string.Empty
        };
    }

    private static string GetTokenText(SyntaxTokenList tokens)
    {
        if (tokens.Count == 0)
            return string.Empty;

        if (tokens.Count == 1)
            return tokens[0].ValueText;

        int capacity = 0;
        foreach (var token in tokens)
        {
            capacity += token.ValueText.Length;
        }

        var sb = new StringBuilder(capacity);
        foreach (var token in tokens)
        {
            sb.Append(token.ValueText);
        }

        return sb.ToString();
    }

    private static string GetInnerText(SyntaxList<XmlNodeSyntax> content)
    {
        if (content.Count == 0)
            return string.Empty;

        var sb = new StringBuilder();
        AppendInnerText(content, sb);
        return sb.ToString();
    }

    private static void AppendInnerText(SyntaxList<XmlNodeSyntax> content, StringBuilder sb)
    {
        foreach (var node in content)
        {
            switch (node)
            {
                case XmlTextSyntax text:
                    foreach (var token in text.TextTokens)
                        sb.Append(token.ValueText);
                    break;
                case XmlCDataSectionSyntax cdata:
                    foreach (var token in cdata.TextTokens)
                        sb.Append(token.ValueText);
                    break;
                case XmlElementSyntax element:
                    AppendInnerText(element.Content, sb);
                    break;
            }
        }
    }

    private static string GetCrefAttributeValue(CrefSyntax cref)
    {
        switch (cref.ToString())
        {
            case "nint":
                return "System.IntPtr";
            case "nuint":
                return "System.UIntPtr";
        }

        if (cref is TypeCrefSyntax { Type: PredefinedTypeSyntax predefinedType } &&
            PredefinedCrefAliases.TryGetValue(predefinedType.Keyword.Kind(), out var alias))
        {
            return alias;
        }

        return cref.ToString();
    }

    private static string GetTextAttributeValue(XmlTextAttributeSyntax textAttr)
    {
        return GetTokenText(textAttr.TextTokens);
    }

    public static XmlTextSyntax? GetAssociatedWhitespaceToRemove(this XmlNodeSyntax xmlNode)
    {
        var content = xmlNode.Parent switch
        {
            DocumentationCommentTriviaSyntax doc => doc.Content,
            XmlElementSyntax element => element.Content,
            _ => default
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

        bool isAtStart = index == 0 || (index == 1 && leading != null && !leading.ContainsNewLine() && leading.IsPureWhitespaceOrPrefix());
        if (isAtStart && trailing.IsPureWhitespaceOrPrefix())
            return trailing;

        if (leading.IsPureWhitespaceOrPrefix())
            return leading;

        if (index + 1 < content.Count - 1 && trailing.IsPureWhitespaceOrPrefix())
            return trailing;

        return null;
    }

    private static bool ContainsNewLine(this XmlTextSyntax xmlText)
    {
        foreach (var token in xmlText.TextTokens)
        {
            if (token.Text.Contains('\n'))
                return true;
        }

        return false;
    }

    public static bool IsPureWhitespaceOrPrefix(this XmlTextSyntax? xmlText)
    {
        if (xmlText == null)
            return false;

        foreach (var token in xmlText.TextTokens)
        {
            if (!IsPureWhitespaceOrPrefix(token.Text.AsSpan()))
                return false;
        }

        return true;
    }

    private static bool IsPureWhitespaceOrPrefix(ReadOnlySpan<char> text)
    {
        if (text.IsEmpty)
            return true;

        int i = 0;
        while (i < text.Length)
        {
            char c = text[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '/')
            {
                if (i + 2 >= text.Length || text[i + 1] != '/' || text[i + 2] != '/')
                    return false;

                i += 3;
                continue;
            }

            if (c == '*')
            {
                i++;
                continue;
            }

            return false;
        }

        return true;
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

    public static string GetIndentation(this MemberDeclarationSyntax member)
    {
        var leadingTrivia = member.GetLeadingTrivia();
        var lastWhitespace = leadingTrivia.LastOrDefault(t => t.IsKind(SyntaxKind.WhitespaceTrivia));
        return lastWhitespace.ToString();
    }

    public static string GetNewLine(this SyntaxNode node)
    {
        var firstNewLine = node.SyntaxTree.GetRoot().DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia)).ToString();
        return !string.IsNullOrEmpty(firstNewLine) ? firstNewLine : Environment.NewLine;
    }

    public static string GetNewLine(this DocumentationCommentTriviaSyntax docTrivia)
    {
        var firstNewLineTrivia = docTrivia.DescendantTrivia().FirstOrDefault(t => t.IsKind(SyntaxKind.EndOfLineTrivia));
        if (firstNewLineTrivia.IsKind(SyntaxKind.EndOfLineTrivia))
            return firstNewLineTrivia.ToString();

        var xmlText = docTrivia.DescendantNodes().OfType<XmlTextSyntax>().FirstOrDefault();
        if (xmlText != null)
        {
            var text = xmlText.ToFullString();
            if (text.Contains("\r\n")) return "\r\n";
            if (text.Contains("\n")) return "\n";
        }

        return ((SyntaxNode)docTrivia).GetNewLine();
    }

    public static string GetPrefix(this DocumentationCommentTriviaSyntax docTrivia)
    {
        return docTrivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia) ? " * " : "/// ";
    }

    public static XmlTextSyntax CreateXmlText(string text)
    {
        return SyntaxFactory.XmlText(SyntaxFactory.TokenList(
            SyntaxFactory.XmlTextLiteral(
                SyntaxTriviaList.Empty,
                text,
                text,
                SyntaxTriviaList.Empty)));
    }

    public static XmlNodeSyntax CreateXmlElement(string tagName, string? attributeValue = null, string content = "")
    {
        XmlAttributeSyntax? attribute = null;
        if (attributeValue != null)
        {
            if (tagName is DocumentationTags.Exception or DocumentationTags.See or DocumentationTags.SeeAlso or DocumentationTags.Permission)
            {
                attribute = SyntaxFactory.XmlCrefAttribute(ParseCref(attributeValue))
                    .WithLeadingTrivia(SyntaxFactory.Whitespace(" "));
            }
            else
            {
                attribute = SyntaxFactory.XmlNameAttribute(
                    SyntaxFactory.XmlName(SyntaxFactory.Identifier(SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")), DocumentationAttributes.Name, SyntaxTriviaList.Empty)),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    SyntaxFactory.IdentifierName(attributeValue),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
            }
        }

        if (string.IsNullOrEmpty(content) && tagName is DocumentationTags.See or DocumentationTags.SeeAlso)
        {
            var emptyElement = SyntaxFactory.XmlEmptyElement(
                SyntaxFactory.Token(SyntaxKind.LessThanToken),
                SyntaxFactory.XmlName(tagName),
                SyntaxFactory.List<XmlAttributeSyntax>(),
                SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken).WithLeadingTrivia(SyntaxFactory.Whitespace(" ")));

            if (attribute != null)
            {
                emptyElement = emptyElement.AddAttributes(attribute);
            }

            return emptyElement;
        }

        var startTag = SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName(tagName));
        if (attribute != null)
        {
            startTag = startTag.AddAttributes(attribute);
        }

        return SyntaxFactory.XmlElement(
            startTag,
            SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(tagName)))
            .WithContent(SyntaxFactory.SingletonList<XmlNodeSyntax>(CreateXmlText(content)));
    }

    public static CrefSyntax ParseCref(string cref)
    {
        if (cref.Contains('<') || cref.Contains('>'))
            cref = cref.Replace('<', '{').Replace('>', '}');

        var tree = CSharpSyntaxTree.ParseText($"/// <see cref=\"{cref}\" />", new CSharpParseOptions(documentationMode: DocumentationMode.Parse));
        var crefAttr = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlCrefAttributeSyntax>().FirstOrDefault();

        if (crefAttr != null && (cref.Length == 0 || crefAttr.Cref.ToString().Length > 0))
            return crefAttr.Cref;

        return SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName(NormalizeCref(cref)));
    }

    public static string NormalizeCref(string cref)
    {
        if (string.IsNullOrEmpty(cref))
            return cref;

        return cref.Contains('{')
            ? cref.Replace('{', '<').Replace('}', '>')
            : cref;
    }
}
