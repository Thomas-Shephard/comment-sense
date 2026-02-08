using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationSyntaxExtensions
{
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

    public static MemberDeclarationSyntax? GetMemberDeclaration(this SyntaxNode? node)
    {
        if (node == null)
            return null;

        var docTrivia = node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        var targetNode = docTrivia != null ? docTrivia.ParentTrivia.Token.Parent : node;
        return targetNode?.FirstAncestorOrSelf<MemberDeclarationSyntax>();
    }
}

