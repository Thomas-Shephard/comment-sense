using System.Collections.Immutable;
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
    bool RequireCapitalization,
    bool ExcludeConstants,
    bool ExcludeEnums,
    double SimilarityThreshold,
    double RenameSimilarityThreshold,
    bool EnableConditionalSuppression,
    bool ScanCalledMethodsForExceptions,
    GhostReferenceMode GhostReferenceMode,
    IReadOnlyDictionary<string, int> TagOrder
)
{
    public static CommentSenseOptions Default { get; } = new(
        VisibilityLevel: VisibilityLevel.Protected,
        AllowImplicitInheritDoc: true,
        LowQualityTerms: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
        Langwords: ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "true", "false", "null", "void"),
        IgnoredExceptions: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
        IgnoreSystemExceptions: false,
        IgnoredExceptionNamespaces: ImmutableHashSet.Create<string>(StringComparer.OrdinalIgnoreCase),
        MinSummaryLength: 0,
        RequireEndingPunctuation: false,
        RequireCapitalization: false,
        ExcludeConstants: false,
        ExcludeEnums: false,
        SimilarityThreshold: 0.0,
        RenameSimilarityThreshold: 0.5,
        EnableConditionalSuppression: false,
        ScanCalledMethodsForExceptions: false,
        GhostReferenceMode: GhostReferenceMode.Safe,
        TagOrder: DocumentationTags.TagOrder
    );

    public static CommentSenseOptions GetOptions(AnalyzerConfigOptionsProvider provider, SyntaxTree tree)
    {
        return CommentSenseOptionsLoader.GetOptions(provider, tree);
    }
}
