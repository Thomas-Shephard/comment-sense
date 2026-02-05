using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class LangwordAnalyzer
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static void Analyze(SyntaxNodeAnalysisContext context, XmlTextSyntax xmlText, CommentSenseOptions options)
    {
        if (options.Langwords.Count == 0)
            return;

        // Skip if inside <code> or <c> tags
        if (IsInsideCodeTag(xmlText))
            return;

        var regex = GetRegex(options.Langwords);

        var matches = xmlText.TextTokens
            .Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken))
            .SelectMany(token => regex.Matches(token.Text).Cast<Match>(), (token, match) => new { token, match });

        foreach (var result in matches)
        {
            var start = result.token.SpanStart + result.match.Index;
            var location = Location.Create(context.Node.SyntaxTree, new Microsoft.CodeAnalysis.Text.TextSpan(start, result.match.Length));
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UseLangwordRule, location, result.match.Value));
        }
    }

    private static Regex GetRegex(IEnumerable<string> langwords)
    {
        var sortedWords = langwords.OrderBy(w => w, StringComparer.OrdinalIgnoreCase).ToList();
        var key = string.Join("|", sortedWords);

        return RegexCache.GetOrAdd(key, _ =>
        {
            var pattern = $@"\b({string.Join("|", sortedWords.OrderByDescending(w => w.Length).Select(Regex.Escape))})\b";
            return new Regex(pattern, RegexOptions.IgnoreCase | RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        });
    }

    private static bool IsInsideCodeTag(XmlTextSyntax xmlText)
    {
        var parent = xmlText.Parent;
        while (parent != null)
        {
            if (parent is XmlElementSyntax element)
            {
                var name = element.StartTag.Name.LocalName.ValueText;
                if (name is "code" or "c")
                    return true;
            }

            parent = parent.Parent;
        }
        return false;
    }
}
