using System.Text.RegularExpressions;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class LangwordAnalyzer
{
    private static readonly System.Runtime.CompilerServices.ConditionalWeakTable<System.Collections.Immutable.IImmutableSet<string>, Regex> RegexCacheBySet = new();

    public static void Analyze(SyntaxNodeAnalysisContext context, XmlTextSyntax xmlText, CommentSenseOptions options)
    {
        if (options.Langwords.Count == 0)
            return;

        // Skip if inside <code> or <c> tags
        if (IsInsideCodeTag(xmlText))
            return;

        var regex = GetRegex(options.Langwords);

        foreach (var token in xmlText.TextTokens)
        {
            if (!token.IsKind(SyntaxKind.XmlTextLiteralToken))
                continue;

            var text = token.Text;
            var matches = regex.Matches(text);
            foreach (Match match in matches)
            {
                var start = token.SpanStart + match.Index;
                var location = Location.Create(context.Node.SyntaxTree, new Microsoft.CodeAnalysis.Text.TextSpan(start, match.Length));
                var matchedText = match.Value;

                // Find canonical form without LINQ.
                // Using a manual loop here is O(N), but Langwords is typically very small.
                string? canonical = null;
                foreach (var word in options.Langwords)
                {
                    if (!string.Equals(word, matchedText, StringComparison.OrdinalIgnoreCase))
                        continue;

                    canonical = word;
                    break;
                }
                canonical ??= matchedText;

                var properties = System.Collections.Immutable.ImmutableDictionary<string, string?>.Empty.Add("canonical", canonical);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UseLangwordRule, location, properties, matchedText));
            }
        }
    }

    private static Regex GetRegex(System.Collections.Immutable.IImmutableSet<string> langwords)
    {
        return RegexCacheBySet.GetValue(langwords, static words =>
        {
            var pattern = $@"\b({string.Join("|", words.OrderByDescending(w => w.Length).ThenBy(w => w, StringComparer.OrdinalIgnoreCase).Select(Regex.Escape))})\b";
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.CultureInvariant, TimeSpan.FromSeconds(1));
        });
    }

    private static bool IsInsideCodeTag(XmlTextSyntax xmlText)
    {
        var parent = xmlText.Parent;
        while (parent != null)
        {
            if (parent is XmlNodeSyntax node)
            {
                var name = node.GetTagName();
                if (name is "code" or "c")
                    return true;
            }

            parent = parent.Parent;
        }
        return false;
    }
}
