using CommentSense.Core;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers;

internal static class CommentSenseRules
{
    private const string Category = "Documentation";

    private static LocalizableResourceString CreateResourceString(string name)
    {
        return new LocalizableResourceString(name, Resources.ResourceManager, typeof(Resources));
    }

    public static readonly DiagnosticDescriptor DisabledDocumentationParsingRule = new(
        CommentSenseDiagnosticIds.DisabledDocumentationParsingId,
        CreateResourceString(nameof(Resources.DisabledDocumentationParsingTitle)),
        CreateResourceString(nameof(Resources.DisabledDocumentationParsingMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.DisabledDocumentationParsingDescription)),
        customTags: WellKnownDiagnosticTags.CompilationEnd);

    public static readonly DiagnosticDescriptor MissingDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingDocumentationId,
        CreateResourceString(nameof(Resources.MissingDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingDocumentationDescription)));

    public static readonly DiagnosticDescriptor MissingParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingParameterDocumentationId,
        CreateResourceString(nameof(Resources.MissingParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor StrayParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayParameterDocumentationId,
        CreateResourceString(nameof(Resources.StrayParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.StrayParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StrayParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor MissingTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId,
        CreateResourceString(nameof(Resources.MissingTypeParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingTypeParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingTypeParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor StrayTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayTypeParameterDocumentationId,
        CreateResourceString(nameof(Resources.StrayTypeParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.StrayTypeParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StrayTypeParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor MissingReturnValueDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingReturnValueDocumentationId,
        CreateResourceString(nameof(Resources.MissingReturnValueDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingReturnValueDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingReturnValueDocumentationDescription)));

    public static readonly DiagnosticDescriptor UnresolvedCrefRule = new(
        CommentSenseDiagnosticIds.UnresolvedCrefId,
        CreateResourceString(nameof(Resources.UnresolvedCrefTitle)),
        CreateResourceString(nameof(Resources.UnresolvedCrefMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.UnresolvedCrefDescription)));

    public static readonly DiagnosticDescriptor ParameterOrderMismatchRule = new(
        CommentSenseDiagnosticIds.ParameterOrderMismatchId,
        CreateResourceString(nameof(Resources.ParameterOrderMismatchTitle)),
        CreateResourceString(nameof(Resources.ParameterOrderMismatchMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.ParameterOrderMismatchDescription)));

    public static readonly DiagnosticDescriptor DuplicateParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.DuplicateParameterDocumentationId,
        CreateResourceString(nameof(Resources.DuplicateParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.DuplicateParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.DuplicateParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor TypeParameterOrderMismatchRule = new(
        CommentSenseDiagnosticIds.TypeParameterOrderMismatchId,
        CreateResourceString(nameof(Resources.TypeParameterOrderMismatchTitle)),
        CreateResourceString(nameof(Resources.TypeParameterOrderMismatchMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.TypeParameterOrderMismatchDescription)));

    public static readonly DiagnosticDescriptor DuplicateTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.DuplicateTypeParameterDocumentationId,
        CreateResourceString(nameof(Resources.DuplicateTypeParameterDocumentationTitle)),
        CreateResourceString(nameof(Resources.DuplicateTypeParameterDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.DuplicateTypeParameterDocumentationDescription)));

    public static readonly DiagnosticDescriptor MissingExceptionDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingExceptionDocumentationId,
        CreateResourceString(nameof(Resources.MissingExceptionDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingExceptionDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingExceptionDocumentationDescription)));

    public static readonly DiagnosticDescriptor StrayReturnValueDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayReturnValueDocumentationId,
        CreateResourceString(nameof(Resources.StrayReturnValueDocumentationTitle)),
        CreateResourceString(nameof(Resources.StrayReturnValueDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StrayReturnValueDocumentationDescription)));

    public static readonly DiagnosticDescriptor MissingValueDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingValueDocumentationId,
        CreateResourceString(nameof(Resources.MissingValueDocumentationTitle)),
        CreateResourceString(nameof(Resources.MissingValueDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: false,
        description: CreateResourceString(nameof(Resources.MissingValueDocumentationDescription)));

    public static readonly DiagnosticDescriptor StrayValueDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayValueDocumentationId,
        CreateResourceString(nameof(Resources.StrayValueDocumentationTitle)),
        CreateResourceString(nameof(Resources.StrayValueDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StrayValueDocumentationDescription)));

    public static readonly DiagnosticDescriptor LowQualityDocumentationRule = new(
        CommentSenseDiagnosticIds.LowQualityDocumentationId,
        CreateResourceString(nameof(Resources.LowQualityDocumentationTitle)),
        CreateResourceString(nameof(Resources.LowQualityDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.LowQualityDocumentationDescription)));

    public static readonly DiagnosticDescriptor InvalidExceptionTypeRule = new(
        CommentSenseDiagnosticIds.InvalidExceptionTypeId,
        CreateResourceString(nameof(Resources.InvalidExceptionTypeTitle)),
        CreateResourceString(nameof(Resources.InvalidExceptionTypeMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.InvalidExceptionTypeDescription)));

    public static readonly DiagnosticDescriptor MissingInheritDocRule = new(
        CommentSenseDiagnosticIds.MissingInheritDocId,
        CreateResourceString(nameof(Resources.MissingInheritDocTitle)),
        CreateResourceString(nameof(Resources.MissingInheritDocMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.MissingInheritDocDescription)));

    public static readonly DiagnosticDescriptor UseLangwordRule = new(
        CommentSenseDiagnosticIds.UseLangwordId,
        CreateResourceString(nameof(Resources.UseLangwordTitle)),
        CreateResourceString(nameof(Resources.UseLangwordMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.UseLangwordDescription)));

    public static readonly DiagnosticDescriptor GhostParameterReferenceRule = new(
        CommentSenseDiagnosticIds.GhostParameterReferenceId,
        CreateResourceString(nameof(Resources.GhostParameterReferenceTitle)),
        CreateResourceString(nameof(Resources.GhostParameterReferenceMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.GhostParameterReferenceDescription)));

    public static readonly DiagnosticDescriptor GhostTypeParameterReferenceRule = new(
        CommentSenseDiagnosticIds.GhostTypeParameterReferenceId,
        CreateResourceString(nameof(Resources.GhostTypeParameterReferenceTitle)),
        CreateResourceString(nameof(Resources.GhostTypeParameterReferenceMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.GhostTypeParameterReferenceDescription)));

    public static readonly DiagnosticDescriptor StraySummaryDocumentationRule = new(
        CommentSenseDiagnosticIds.StraySummaryDocumentationId,
        CreateResourceString(nameof(Resources.StraySummaryDocumentationTitle)),
        CreateResourceString(nameof(Resources.StraySummaryDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StraySummaryDocumentationDescription)));

    public static readonly DiagnosticDescriptor StrayExceptionDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayExceptionDocumentationId,
        CreateResourceString(nameof(Resources.StrayExceptionDocumentationTitle)),
        CreateResourceString(nameof(Resources.StrayExceptionDocumentationMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.StrayExceptionDocumentationDescription)));

    public static readonly DiagnosticDescriptor DocumentationTagOrderMismatchRule = new(
        CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId,
        CreateResourceString(nameof(Resources.DocumentationTagOrderMismatchTitle)),
        CreateResourceString(nameof(Resources.DocumentationTagOrderMismatchMessage)),
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: CreateResourceString(nameof(Resources.DocumentationTagOrderMismatchDescription)));

    public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =
    [
        DisabledDocumentationParsingRule,
        MissingDocumentationRule,
        MissingParameterDocumentationRule,
        StrayParameterDocumentationRule,
        MissingTypeParameterDocumentationRule,
        StrayTypeParameterDocumentationRule,
        MissingReturnValueDocumentationRule,
        UnresolvedCrefRule,
        ParameterOrderMismatchRule,
        DuplicateParameterDocumentationRule,
        TypeParameterOrderMismatchRule,
        DuplicateTypeParameterDocumentationRule,
        MissingExceptionDocumentationRule,
        StrayReturnValueDocumentationRule,
        MissingValueDocumentationRule,
        StrayValueDocumentationRule,
        LowQualityDocumentationRule,
        InvalidExceptionTypeRule,
        MissingInheritDocRule,
        UseLangwordRule,
        GhostParameterReferenceRule,
        GhostTypeParameterReferenceRule,
        StraySummaryDocumentationRule,
        StrayExceptionDocumentationRule,
        DocumentationTagOrderMismatchRule
    ];
}
