using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Core;

internal static class CommentSenseOptionsLoader
{
    private const string Prefix = "comment_sense.";

    private static readonly ConditionalWeakTable<AnalyzerConfigOptions, CommentSenseOptions> OptionsCache = new();

    public static CommentSenseOptions GetOptions(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        var options = provider.GetOptions(tree);
        return OptionsCache.GetValue(options, o => FromAnalyzerConfigOptions(o, provider.GlobalOptions));
    }

    private static CommentSenseOptions FromAnalyzerConfigOptions(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions)
    {
        var visibilityLevel = GetEnumOption(options, globalOptions, "visibility_level", CommentSenseOptions.Default.VisibilityLevel);

        // Backward compatibility for analyze_internal:
        if (!HasOption(options, globalOptions, "visibility_level") && GetBoolOption(options, globalOptions, "analyze_internal"))
            visibilityLevel = VisibilityLevel.Internal;

        var allowImplicitInheritDoc = GetBoolOption(options, globalOptions, "allow_implicit_inheritdoc", CommentSenseOptions.Default.AllowImplicitInheritDoc);
        var lowQualityTerms = GetSetOption(options, globalOptions, "low_quality_terms", CommentSenseOptions.Default.LowQualityTerms);
        var langwords = GetSetOption(options, globalOptions, "langwords", CommentSenseOptions.Default.Langwords);
        var ignoredExceptions = GetSetOption(options, globalOptions, "ignored_exceptions", CommentSenseOptions.Default.IgnoredExceptions);
        var ignoreSystemExceptions = GetBoolOption(options, globalOptions, "ignore_system_exceptions", CommentSenseOptions.Default.IgnoreSystemExceptions);
        var ignoredExceptionNamespaces = GetSetOption(options, globalOptions, "ignored_exception_namespaces", CommentSenseOptions.Default.IgnoredExceptionNamespaces);
        var minSummaryLength = GetIntOption(options, globalOptions, "min_summary_length", CommentSenseOptions.Default.MinSummaryLength);
        var requireEndingPunctuation = GetBoolOption(options, globalOptions, "require_ending_punctuation", CommentSenseOptions.Default.RequireEndingPunctuation);
        var requireCapitalization = GetBoolOption(options, globalOptions, "require_capitalization", CommentSenseOptions.Default.RequireCapitalization);
        var requirePropertyPatterns = GetBoolOption(options, globalOptions, "require_property_patterns", CommentSenseOptions.Default.RequirePropertyPatterns);
        var excludeConstants = GetBoolOption(options, globalOptions, "exclude_constants", CommentSenseOptions.Default.ExcludeConstants);
        var excludeEnums = GetBoolOption(options, globalOptions, "exclude_enums", CommentSenseOptions.Default.ExcludeEnums);

        var rawThreshold = GetDoubleOption(options, globalOptions, "similarity_threshold", CommentSenseOptions.Default.SimilarityThreshold);
        var similarityThreshold = Math.Max(0.0, Math.Min(1.0, rawThreshold));

        var rawRenameThreshold = GetDoubleOption(options, globalOptions, "rename_similarity_threshold", CommentSenseOptions.Default.RenameSimilarityThreshold);
        var renameSimilarityThreshold = Math.Max(0.0, Math.Min(1.0, rawRenameThreshold));

        var enableSuppression = GetBoolOption(options, globalOptions, "enable_conditional_suppression", CommentSenseOptions.Default.EnableConditionalSuppression);
        var scanExceptions = GetBoolOption(options, globalOptions, "scan_called_methods_for_exceptions", CommentSenseOptions.Default.ScanCalledMethodsForExceptions);
        var ghostMode = GetEnumOption(options, globalOptions, "ghost_references.mode", CommentSenseOptions.Default.GhostReferenceMode);
        var tagOrder = GetTagOrderOption(options, globalOptions, "tag_order", CommentSenseOptions.Default.TagOrder);

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
            requireCapitalization,
            requirePropertyPatterns,
            excludeConstants,
            excludeEnums,
            similarityThreshold,
            renameSimilarityThreshold,
            enableSuppression,
            scanExceptions,
            ghostMode,
            tagOrder
        );
    }

    internal static IReadOnlyDictionary<string, int> GetTagOrderOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, IReadOnlyDictionary<string, int> defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value))
            return ParseTagOrder(value);

        if (globalOptions.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value))
            return ParseTagOrder(value);

        return defaultValue;
    }

    private static Dictionary<string, int> ParseTagOrder(string value)
    {
        var tags = value.Split(',')
            .Select(t => t.Trim().ToLowerInvariant())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        var result = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < tags.Count; i++)
        {
            if (!result.ContainsKey(tags[i]))
            {
                result[tags[i]] = i;
            }
        }

        // Special case: if returns is present but value is not, give value the same priority
        if (result.TryGetValue(DocumentationTags.Returns, out var returnsOrder) && !result.ContainsKey(DocumentationTags.Value))
        {
            result[DocumentationTags.Value] = returnsOrder;
        }
        else if (result.TryGetValue(DocumentationTags.Value, out var valueOrder) && !result.ContainsKey(DocumentationTags.Returns))
        {
            result[DocumentationTags.Returns] = valueOrder;
        }

        return result;
    }

    internal static bool HasOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name)
    {
        var key = Prefix + name;
        return options.TryGetValue(key, out _) || globalOptions.TryGetValue(key, out _);
    }

    internal static T GetEnumOption<T>(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, T defaultValue) where T : struct, Enum
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse<T>(value, true, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && !string.IsNullOrWhiteSpace(value) && Enum.TryParse(value, true, out result))
            return result;

        return defaultValue;
    }

    internal static bool GetBoolOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, bool defaultValue = false)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && bool.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && bool.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    internal static int GetIntOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, int defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && int.TryParse(value, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && int.TryParse(value, out result))
            return result;

        return defaultValue;
    }

    internal static double GetDoubleOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, double defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var result))
            return result;

        if (globalOptions.TryGetValue(key, out value) && double.TryParse(value, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out result))
            return result;

        return defaultValue;
    }

    internal static IImmutableSet<string> GetSetOption(AnalyzerConfigOptions options, AnalyzerConfigOptions globalOptions, string name, IImmutableSet<string> defaultValue)
    {
        var key = Prefix + name;
        if (options.TryGetValue(key, out var value))
            return ParseSet(value);

        if (globalOptions.TryGetValue(key, out value))
            return ParseSet(value);

        return defaultValue;
    }

    internal static ImmutableHashSet<string> ParseSet(string value)
    {
        return value.Split(',')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToImmutableHashSet(StringComparer.OrdinalIgnoreCase);
    }
}
