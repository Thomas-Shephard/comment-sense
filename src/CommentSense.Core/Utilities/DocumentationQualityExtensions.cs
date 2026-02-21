using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationQualityExtensions
{
    public enum PunctuationState { Yes, No, Meaningless }

    public static bool EndsWithPunctuation(this SyntaxList<XmlNodeSyntax> content) => content.GetPunctuationState() == PunctuationState.Yes;

    public static bool EndsWithPunctuation(string content, bool trimEnd = true) => EndsWithPunctuation(content.AsSpan(), trimEnd);

    public static bool EndsWithPunctuation(ReadOnlySpan<char> content, bool trimEnd = true) => GetPunctuationState(content, trimEnd) == PunctuationState.Yes;

    public static PunctuationState GetPunctuationState(this SyntaxList<XmlNodeSyntax> content)
    {
        for (int i = content.Count - 1; i >= 0; i--)
        {
            var state = content[i].GetPunctuationState();
            if (state != PunctuationState.Meaningless)
                return state;
        }

        return PunctuationState.Meaningless;
    }

    public static PunctuationState GetPunctuationState(this XmlNodeSyntax node)
    {
        if (node is XmlTextSyntax or XmlCDataSectionSyntax)
        {
            var tokens = node.GetTextTokens();
            for (int j = tokens.Count - 1; j >= 0; j--)
            {
                var state = GetPunctuationState(tokens[j].ValueText.AsSpan(), trimEnd: true);
                if (state != PunctuationState.Meaningless)
                    return state;
            }

            return PunctuationState.Meaningless;
        }

        if (node is XmlElementSyntax xmlElement)
            return xmlElement.Content.GetPunctuationState();

        if (node is XmlEmptyElementSyntax emptyElement)
            return emptyElement.Attributes.Count == 0 ? PunctuationState.Meaningless : PunctuationState.No;

        return PunctuationState.Meaningless;
    }

    public static PunctuationState GetPunctuationState(string content, bool trimEnd = true) => GetPunctuationState(content.AsSpan(), trimEnd);

    public static PunctuationState GetPunctuationState(ReadOnlySpan<char> content, bool trimEnd = true)
    {
        if (content.IsEmpty)
            return PunctuationState.Meaningless;

        int index = content.Length - 1;
        if (trimEnd)
        {
            while (index >= 0 && char.IsWhiteSpace(content[index]))
            {
                index--;
            }
        }

        if (index < 0)
            return PunctuationState.Meaningless;

        return content[index] switch
        {
            '.' or '!' or '?' => PunctuationState.Yes,
            _ => PunctuationState.No
        };
    }

    public static bool HasAnyLetter(this SyntaxList<XmlNodeSyntax> content)
    {
        foreach (var node in content)
        {
            if (node is XmlTextSyntax or XmlCDataSectionSyntax)
            {
                foreach (var token in node.GetTextTokens())
                {
                    if (HasAnyLetter(token.ValueText.AsSpan()))
                        return true;
                }
            }
            else if (node is XmlElementSyntax xmlElement && xmlElement.Content.HasAnyLetter())
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasAnyLetter(ReadOnlySpan<char> content)
    {
        foreach (char c in content)
        {
            if (char.IsLetter(c))
                return true;
        }

        return false;
    }

    public static bool StartsWithLowercase(string content) => StartsWithLowercase(content.AsSpan());

    public static bool StartsWithLowercase(ReadOnlySpan<char> content)
    {
        foreach (char c in content)
        {
            if (char.IsWhiteSpace(c))
                continue;

            return char.IsLetter(c) && char.IsLower(c);
        }

        return false;
    }

    public static SyntaxTokenList GetTextTokens(this XmlNodeSyntax node) => node switch
    {
        XmlTextSyntax t => t.TextTokens,
        XmlCDataSectionSyntax c => c.TextTokens,
        _ => default
    };

    public static XmlNodeSyntax WithTextTokens(this XmlNodeSyntax node, SyntaxTokenList tokens) => node switch
    {
        XmlTextSyntax t => t.WithTextTokens(tokens),
        XmlCDataSectionSyntax c => c.WithTextTokens(tokens),
        _ => node
    };
}
