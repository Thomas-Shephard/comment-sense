using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationSyntaxExtensions
{
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
                return string.Concat(textAttr.TextTokens.Select(t => t.ValueText));
        }

        return null;
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
