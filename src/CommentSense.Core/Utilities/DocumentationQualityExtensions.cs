using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationQualityExtensions
{
    private static readonly char[] SentenceTerminators = ['.', '!', '?'];

    public enum PunctuationState { Yes, No, Meaningless }

    public static bool EndsWithPunctuation(this SyntaxList<XmlNodeSyntax> content) => content.GetPunctuationState() == PunctuationState.Yes;

    public static bool EndsWithPunctuation(string content, bool trimEnd = true) => GetPunctuationState(content, trimEnd) == PunctuationState.Yes;

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
                var state = GetPunctuationState(tokens[j].ValueText, trimEnd: true);
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

    public static PunctuationState GetPunctuationState(string content, bool trimEnd = true)
    {
        if (string.IsNullOrEmpty(content))
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

        return SentenceTerminators.Contains(content[index])
            ? PunctuationState.Yes
            : PunctuationState.No;
    }

    public static bool HasAnyLetter(this SyntaxList<XmlNodeSyntax> content)
    {
        foreach (var node in content)
        {
            switch (node)
            {
                case XmlTextSyntax or XmlCDataSectionSyntax when node.GetTextTokens().Any(token => token.ValueText.Any(char.IsLetter)):
                case XmlElementSyntax xmlElement when xmlElement.Content.HasAnyLetter():
                    return true;
            }
        }

        return false;
    }

    public static bool StartsWithLowercase(string content)
    {
        for (int i = 0; i < content.Length; i++)
        {
            if (char.IsWhiteSpace(content[i]))
                continue;

            return char.IsLetter(content, i) && char.IsLower(content, i);
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
