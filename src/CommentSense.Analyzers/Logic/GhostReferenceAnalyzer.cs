using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Text.RegularExpressions;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class GhostReferenceAnalyzer
{
    private static readonly ConcurrentDictionary<string, Regex> RegexCache = new();

    public static void Analyze(SyntaxNodeAnalysisContext context, XmlTextSyntax xmlText, ISymbol symbol, CommentSenseOptions options)
    {
        if (options.GhostReferenceMode == GhostReferenceMode.Off)
            return;

        var (containingTag, nameValue) = GetContainingTagInfo(xmlText);

        if (IsIgnoredTag(containingTag))
            return;

        var parameters = GetParameters(symbol);
        var typeParameters = GetTypeParameters(symbol);

        if (parameters.IsEmpty && typeParameters.IsEmpty)
            return;

        AnalyzeReferences(context, xmlText, parameters, CommentSenseRules.GhostParameterReferenceRule, options, containingTag, nameValue);
        AnalyzeReferences(context, xmlText, typeParameters, CommentSenseRules.GhostTypeParameterReferenceRule, options, containingTag, nameValue);
    }

    private static void AnalyzeReferences(
        SyntaxNodeAnalysisContext context,
        XmlTextSyntax xmlText,
        ImmutableArray<string> names,
        DiagnosticDescriptor rule,
        CommentSenseOptions options,
        string? containingTag,
        string? nameValue)
    {
        if (names.IsEmpty)
            return;

        var regex = GetRegex(names);
        var matches = xmlText.TextTokens
            .Where(t => t.IsKind(SyntaxKind.XmlTextLiteralToken))
            .SelectMany(token => regex.Matches(token.Text).Cast<Match>(), (token, match) => new { token, match });

        foreach (var result in matches)
        {
            var name = result.match.Value;

            if (!IsEligible(name, options.GhostReferenceMode))
                continue;

            if (options.GhostReferenceMode == GhostReferenceMode.Safe)
            {
                if (containingTag == "param" && name == nameValue) continue;
                if (containingTag == "typeparam" && name == nameValue) continue;
            }

            var start = result.token.SpanStart + result.match.Index;
            var location = Location.Create(context.Node.SyntaxTree, new Microsoft.CodeAnalysis.Text.TextSpan(start, result.match.Length));
            context.ReportDiagnostic(Diagnostic.Create(rule, location, name));
        }
    }

    private static bool IsEligible(string name, GhostReferenceMode mode)
    {
        if (mode == GhostReferenceMode.Strict)
            return true;

        return name.Any(char.IsUpper) || name.Contains('_') || name.Any(char.IsDigit);
    }

    private static Regex GetRegex(ImmutableArray<string> names)
    {
        var sortedForCache = names.OrderBy(n => n).ToList();
        var key = string.Join("|", sortedForCache);

        return RegexCache.GetOrAdd(key, _ =>
        {
            var pattern = $@"\b({string.Join("|", names.OrderByDescending(w => w.Length).Select(Regex.Escape))})\b";
            return new Regex(pattern, RegexOptions.Compiled, TimeSpan.FromSeconds(1));
        });
    }

    private static bool IsIgnoredTag(string? tagName)
    {
        return tagName is "code" or "c" or "paramref" or "typeparamref" or "see";
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

            var nameValue = GetNameAttributeValue(element);
            if (nameValue != null && tagName is "param" or "typeparam")
                return (tagName, nameValue);

            if (IsIgnoredTag(tagName))
                return (tagName, null);
        }

        return (innermostTag, null);
    }

    private static string? GetNameAttributeValue(XmlElementSyntax element)
    {
        foreach (var attribute in element.StartTag.Attributes)
        {
            if (attribute is XmlNameAttributeSyntax { Name.LocalName.ValueText: "name" } nameAttr)
                return nameAttr.Identifier.Identifier.ValueText;

            if (attribute is XmlTextAttributeSyntax { Name.LocalName.ValueText: "name" } textAttr)
            {
                return textAttr.TextTokens
                    .FirstOrDefault(t => t.IsKind(SyntaxKind.XmlTextLiteralToken))
                    .Text;
            }
        }

        return null;
    }

    private static ImmutableArray<string> GetParameters(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => [.. m.Parameters.Select(p => p.Name)],
            IPropertySymbol { IsIndexer: true } p => [.. p.Parameters.Select(param => param.Name)],
            INamedTypeSymbol { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null } n => [.. n.DelegateInvokeMethod.Parameters.Select(p => p.Name)],
            _ => []
        };
    }

    private static ImmutableArray<string> GetTypeParameters(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => [.. m.TypeParameters.Select(p => p.Name)],
            INamedTypeSymbol n => [.. n.TypeParameters.Select(p => p.Name)],
            _ => []
        };
    }
}