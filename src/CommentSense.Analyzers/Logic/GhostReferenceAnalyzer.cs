using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CommentSense.Analyzers.Logic;

internal static class GhostReferenceAnalyzer
{
    private static readonly ConcurrentDictionary<ImmutableArray<string>, Regex> RegexCache = new(new NameListComparer());

    public static void Analyze(SyntaxNodeAnalysisContext context, XmlTextSyntax xmlText, ISymbol symbol, CommentSenseOptions options)
    {
        if (options.GhostReferenceMode == GhostReferenceMode.Off)
            return;

        var (containingTag, nameValue) = GetContainingTagInfo(xmlText);

        if (IsIgnoredTag(containingTag))
            return;

        var parameters = symbol.GetParameters();
        var typeParameters = symbol.GetTypeParameters();

        if (parameters.IsEmpty && typeParameters.IsEmpty)
            return;

        var parameterNames = parameters.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToImmutableArray();
        var typeParameterNames = typeParameters.Select(p => p.Name).OrderBy(n => n, StringComparer.OrdinalIgnoreCase).ToImmutableArray();

        var reportedSpans = new HashSet<TextSpan>();
        var analysisContext = new GhostReferenceContext(context, xmlText, options, containingTag, nameValue, reportedSpans);

        AnalyzeReferences(analysisContext, parameterNames, CommentSenseRules.GhostParameterReferenceRule);
        AnalyzeReferences(analysisContext, typeParameterNames, CommentSenseRules.GhostTypeParameterReferenceRule);
    }

    private readonly record struct GhostReferenceContext(
        SyntaxNodeAnalysisContext AnalysisContext,
        XmlTextSyntax XmlText,
        CommentSenseOptions Options,
        string? ContainingTag,
        string? NameValue,
        HashSet<TextSpan> ReportedSpans);

    private static void AnalyzeReferences(
        GhostReferenceContext context,
        ImmutableArray<string> names,
        DiagnosticDescriptor rule)
    {
        if (names.IsEmpty)
            return;

        // Fast-path for extremely large parameter sets or long documentation blocks to avoid
        if (names.Length > 100 || context.XmlText.Span.Length > 50000)
        {
            AnalyzeReferencesFast(context, names, rule);
            return;
        }

        var regex = GetRegex(names);
        foreach (var token in context.XmlText.TextTokens.Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken)))
        {
            foreach (Match match in regex.Matches(token.Text))
            {
                var matchedText = match.Value;
                var originalName = ResolveOriginalName(matchedText, names);

                if (originalName == null || !IsGhostReference(matchedText, originalName, context.Options, context.ContainingTag, context.NameValue))
                    continue;

                var start = token.SpanStart + match.Index;
                var span = new TextSpan(start, match.Length);

                if (!context.ReportedSpans.Add(span))
                    continue;

                var location = Location.Create(context.AnalysisContext.Node.SyntaxTree, span);
                var properties = ImmutableDictionary<string, string?>.Empty.Add("originalName", originalName);
                context.AnalysisContext.ReportDiagnostic(Diagnostic.Create(rule, location, properties, matchedText, originalName));
            }
        }
    }

    private static void AnalyzeReferencesFast(
        GhostReferenceContext context,
        ImmutableArray<string> names,
        DiagnosticDescriptor rule)
    {
        var exactMatchMap = names.ToDictionary(n => n, StringComparer.Ordinal);
        var caseInsensitiveMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var name in names)
        {
            if (!caseInsensitiveMap.ContainsKey(name))
                caseInsensitiveMap[name] = name;
        }

        var lookups = new FastPathLookups(exactMatchMap, caseInsensitiveMap, rule);

        foreach (var token in context.XmlText.TextTokens.Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken)))
        {
            AnalyzeTokenFast(token, lookups, context);
        }
    }

    private readonly record struct FastPathLookups(
        Dictionary<string, string> ExactMatchMap,
        Dictionary<string, string> CaseInsensitiveMap,
        DiagnosticDescriptor Rule);

    private static void AnalyzeTokenFast(
        SyntaxToken token,
        FastPathLookups lookups,
        GhostReferenceContext context)
    {
        var text = token.Text;
        var start = -1;
        for (var i = 0; i < text.Length; i++)
        {
            var isWordChar = char.IsLetterOrDigit(text[i]) || text[i] == '_';
            if (isWordChar && start == -1)
            {
                start = i;
            }
            else if (!isWordChar && start != -1)
            {
                ReportIfMatch(token, text, start, i - start, lookups, context);
                start = -1;
            }
        }

        if (start != -1)
            ReportIfMatch(token, text, start, text.Length - start, lookups, context);
    }

    private static void ReportIfMatch(
        SyntaxToken token,
        string text,
        int wordStart,
        int wordLength,
        FastPathLookups lookups,
        GhostReferenceContext context)
    {
        var word = text.Substring(wordStart, wordLength);
        if (!lookups.ExactMatchMap.TryGetValue(word, out var originalName) && !lookups.CaseInsensitiveMap.TryGetValue(word, out originalName))
            return;

        if (!IsGhostReference(word, originalName, context.Options, context.ContainingTag, context.NameValue))
            return;

        var absoluteStart = token.SpanStart + wordStart;
        var span = new TextSpan(absoluteStart, wordLength);

        if (!context.ReportedSpans.Add(span))
            return;

        var location = Location.Create(context.AnalysisContext.Node.SyntaxTree, span);
        var properties = ImmutableDictionary<string, string?>.Empty.Add("originalName", originalName);
        context.AnalysisContext.ReportDiagnostic(Diagnostic.Create(lookups.Rule, location, properties, word, originalName));
    }

    private static string? ResolveOriginalName(string matchedText, ImmutableArray<string> names)
    {
        return names.FirstOrDefault(n => string.Equals(n, matchedText, StringComparison.Ordinal))
               ?? names.FirstOrDefault(n => string.Equals(n, matchedText, StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsGhostReference(string matchedText, string originalName, CommentSenseOptions options, string? containingTag, string? nameValue)
    {
        if (!IsEligible(matchedText, options.GhostReferenceMode) &&
            (matchedText == originalName || !IsEligible(originalName, options.GhostReferenceMode)))
            return false;

        if (options.GhostReferenceMode == GhostReferenceMode.Safe)
        {
            if (containingTag == DocumentationTags.Param && string.Equals(originalName, nameValue, StringComparison.Ordinal)) return false;
            if (containingTag == DocumentationTags.TypeParam && string.Equals(originalName, nameValue, StringComparison.Ordinal)) return false;
        }

        return true;
    }

    private static bool IsEligible(string name, GhostReferenceMode mode)
    {
        if (mode == GhostReferenceMode.Strict)
            return true;

        return name.Any(char.IsUpper) || name.Contains('_') || name.Any(char.IsDigit);
    }

    private static Regex GetRegex(ImmutableArray<string> names)
    {
        return RegexCache.GetOrAdd(names, static n =>
        {
            var pattern = $@"\b({string.Join("|", n.OrderByDescending(w => w.Length).Select(Regex.Escape))})\b";
            return new Regex(pattern, RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
        });
    }

    private sealed class NameListComparer : IEqualityComparer<ImmutableArray<string>>
    {
        public bool Equals(ImmutableArray<string> x, ImmutableArray<string> y)
        {
            if (x.Length != y.Length)
                return false;

            for (var i = 0; i < x.Length; i++)
            {
                if (!string.Equals(x[i], y[i], StringComparison.OrdinalIgnoreCase))
                    return false;
            }

            return true;
        }

        public int GetHashCode(ImmutableArray<string> obj)
        {
            var hash = 17;
            foreach (string t in obj)
            {
                hash = hash * 31 + StringComparer.OrdinalIgnoreCase.GetHashCode(t);
            }

            return hash;
        }
    }

    private static bool IsIgnoredTag(string? tagName)
    {
        return tagName is DocumentationTags.Code or DocumentationTags.C or DocumentationTags.ParamRef or DocumentationTags.TypeParamRef or DocumentationTags.See;
    }

    private static (string? TagName, string? NameValue) GetContainingTagInfo(XmlTextSyntax xmlText)
    {
        string? innermostTag = null;
        for (var parent = xmlText.Parent; parent != null; parent = parent.Parent)
        {
            if (parent is not XmlElementSyntax element)
                continue;

            var tagName = element.StartTag.Name.LocalName.ValueText;
            innermostTag ??= tagName;

            var nameValue = element.GetNameAttribute();
            if (nameValue != null && tagName is DocumentationTags.Param or DocumentationTags.TypeParam)
                return (tagName, nameValue);

            if (IsIgnoredTag(tagName))
                return (tagName, null);
        }

        return (innermostTag, null);
    }
}
