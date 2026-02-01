using System.Collections.Immutable;
using CommentSense.Core;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers;

internal static class CommentSenseSuppressions
{
    private static LocalizableResourceString CreateResourceString(string name)
    {
        return new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
    }

    private static readonly SuppressionDescriptor MissingXmlCommentSuppression = new(
        CommentSenseDiagnosticIds.SuppressMissingXmlCommentId,
        "CS1591",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor MissingParamTagSuppression = new(
        CommentSenseDiagnosticIds.SuppressMissingParamTagId,
        "CS1573",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor StrayParamTagSuppression = new(
        CommentSenseDiagnosticIds.SuppressStrayParamTagId,
        "CS1572",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor DuplicateParamTagSuppression = new(
        CommentSenseDiagnosticIds.SuppressDuplicateParamTagId,
        "CS1571",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor InvalidCrefSuppression = new(
        CommentSenseDiagnosticIds.SuppressInvalidCrefId,
        "CS1584",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor UnresolvedCrefSuppression = new(
        CommentSenseDiagnosticIds.SuppressUnresolvedCrefId,
        "CS1574",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    private static readonly SuppressionDescriptor InvalidCrefSecondarySuppression = new(
        CommentSenseDiagnosticIds.SuppressInvalidCrefSecondaryId,
        "CS1658",
        CreateResourceString(nameof(Resources.SuppressionJustification)));

    public static readonly ImmutableArray<SuppressionDescriptor> SupportedSuppressions =
    [
        MissingXmlCommentSuppression,
        MissingParamTagSuppression,
        StrayParamTagSuppression,
        DuplicateParamTagSuppression,
        InvalidCrefSuppression,
        UnresolvedCrefSuppression,
        InvalidCrefSecondarySuppression
    ];

    public static readonly ImmutableDictionary<string, SuppressionDescriptor> SuppressionMap =
        SupportedSuppressions.ToImmutableDictionary(d => d.SuppressedDiagnosticId);
}
