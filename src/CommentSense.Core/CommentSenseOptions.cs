using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Core;

internal sealed record CommentSenseOptions(
    VisibilityLevel VisibilityLevel,
    bool AllowImplicitInheritDoc,
    IImmutableSet<string> LowQualityTerms,
    IImmutableSet<string> Langwords,
    IImmutableSet<string> IgnoredExceptions,
    bool IgnoreSystemExceptions,
    IImmutableSet<string> IgnoredExceptionNamespaces,
    int MinSummaryLength,
    bool RequireEndingPunctuation,
    bool ExcludeConstants,
    bool ExcludeEnums,
    double SimilarityThreshold,
    bool EnableConditionalSuppression,
    bool ScanCalledMethodsForExceptions,
    GhostReferenceMode GhostReferenceMode
)
{
    private const string Prefix = "comment_sense.";

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, CommentSenseOptions> OptionsCache = new();

    private static readonly ImmutableHashSet<string> DefaultLangwords = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "true", "false", "null", "void");
    private static readonly ImmutableHashSet<string> EmptySet = ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase);

    public static CommentSenseOptions Default { get; } = new(
        VisibilityLevel: VisibilityLevel.Protected,
        AllowImplicitInheritDoc: true,
        LowQualityTerms: EmptySet,
        Langwords: DefaultLangwords,
        IgnoredExceptions: EmptySet,
        IgnoreSystemExceptions: false,
        IgnoredExceptionNamespaces: EmptySet,
        MinSummaryLength: 0,
        RequireEndingPunctuation: false,
        ExcludeConstants: false,
        ExcludeEnums: false,
        SimilarityThreshold: 0.0,
        EnableConditionalSuppression: false,
        ScanCalledMethodsForExceptions: false,
        GhostReferenceMode: GhostReferenceMode.Safe
    );

    public static CommentSenseOptions GetOptions(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        var options = provider.GetOptions(tree);
        return OptionsCache.GetValue(options, o => FromAnalyzerConfigOptions(o, provider.GlobalOptions));
    }

    private static CommentSenseOptions FromAnalyzerConfigOptions(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions)
    {
        var visibilityLevel = GetEnumOption(options, globalOptions, "visibility_level", VisibilityLevel.Protected);

        // Backward compatibility for analyze_internal:
        if (!HasOption(options, globalOptions, "visibility_level") && GetBoolOption(options, globalOptions, "analyze_internal"))
            visibilityLevel = VisibilityLevel.Internal;

        var allowImplicitInheritDoc = GetBoolOption(options, globalOptions, "allow_implicit_inheritdoc", true);
        var lowQualityTerms = GetSetOption(options, globalOptions, "low_quality_terms", EmptySet);
        var langwords = GetSetOption(options, globalOptions, "langwords", DefaultLangwords);
        var ignoredExceptions = GetSetOption(options, globalOptions, "ignored_exceptions", EmptySet);
        var ignoreSystemExceptions = GetBoolOption(options, globalOptions, "ignore_system_exceptions");
        var ignoredExceptionNamespaces = GetSetOption(options, globalOptions, "ignored_exception_namespaces", EmptySet);
        var minSummaryLength = GetIntOption(options, globalOptions, "min_summary_length", 0);
        var requireEndingPunctuation = GetBoolOption(options, globalOptions, "require_ending_punctuation");
        var excludeConstants = GetBoolOption(options, globalOptions, "exclude_constants");
        var excludeEnums = GetBoolOption(options, globalOptions, "exclude_enums");

        var rawThreshold = GetDoubleOption(options, globalOptions, "similarity_threshold", 0.0);
        var similarityThreshold = Math.Max(0.0, Math.Min(1.0, rawThreshold));

        var enableSuppression = GetBoolOption(options, globalOptions, "enable_conditional_suppression");
        var scanExceptions = GetBoolOption(options, globalOptions, "scan_called_methods_for_exceptions");
        var ghostMode = GetEnumOption(options, globalOptions, "ghost_references.mode", GhostReferenceMode.Safe);

        return new CommentSenseOptions(
            visibilityLevel,
            allowImplicitInheritDoc,
            lowQualityTerms,
            langwords,
            ignoredExceptions,
            ignoreSystemExceptions,
            ignoredExceptionNamespaces,
            minSummaryLength,
            requireEndingPunctuation,
            excludeConstants,
            excludeEnums,
            similarityThreshold,
            enableSuppression,
            scanExceptions,
            ghostMode
        );
    }

    private static bool HasOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name)
    {
        var key = Prefix + name;
        return options.TryGetValue(key, out _) || globalOptions.TryGetValue(key, out _);
    }

    private static T GetEnumOption<T>(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, T defaultValue) where T : struct, Enum
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out result))
            return result;

        return defaultValue;
    }

    private static bool GetBoolOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, bool defaultValue = false)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && bool.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    private static int GetIntOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, int defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && int.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    private static double GetDoubleOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, double defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            return result;

        return defaultValue;
    }

    private static IImmutableSet<string> GetSetOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, IImmutableSet<string> defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value))
            return ParseSet(value);

        if (globalOptions.TryGetValue(key, out value))
            return ParseSet(value);

        return defaultValue;
    }

    private static ImmutableHashSet<string> ParseSet(string value)
    {
        var terms = value.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);

        return terms;
    }
}
