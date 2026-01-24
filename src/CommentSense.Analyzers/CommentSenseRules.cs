using CommentSense.Core;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers;

internal static class CommentSenseRules
{
    private const string Category = "Documentation";

    public static readonly DiagnosticDescriptor MissingDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingDocumentationId,
        "Public API is missing documentation",
        "The symbol '{0}' is missing valid documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Publicly accessible APIs should be documented to ensure maintainability and clarity.");

    public static readonly DiagnosticDescriptor MissingParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingParameterDocumentationId,
        "Missing parameter documentation",
        "The parameter '{0}' is missing documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All parameters of a publicly accessible member should be documented.");

    public static readonly DiagnosticDescriptor StrayParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayParameterDocumentationId,
        "Stray parameter documentation",
        "The parameter '{0}' in the documentation does not exist in the method signature",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Documentation should not contain <param> tags for parameters that do not exist.");

    public static readonly DiagnosticDescriptor MissingTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId,
        "Missing type parameter documentation",
        "The type parameter '{0}' is missing documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All type parameters of a publicly accessible member should be documented.");

    public static readonly DiagnosticDescriptor StrayTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayTypeParameterDocumentationId,
        "Stray type parameter documentation",
        "The type parameter '{0}' in the documentation does not exist in the signature",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Documentation should not contain <typeparam> tags for type parameters that do not exist.");

    public static readonly DiagnosticDescriptor MissingReturnValueDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingReturnValueDocumentationId,
        "Missing return value documentation",
        "The method '{0}' is missing return value documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Non-void methods of a publicly accessible member should have a <returns> tag to document the return value.");

    public static readonly DiagnosticDescriptor UnresolvedCrefRule = new(
        CommentSenseDiagnosticIds.UnresolvedCrefId,
        "Invalid XML documentation reference",
        "The cref reference '{0}' could not be resolved",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The 'cref' attribute in XML documentation must refer to a valid symbol.");

    public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = [
        MissingDocumentationRule,
        MissingParameterDocumentationRule,
        StrayParameterDocumentationRule,
        MissingTypeParameterDocumentationRule,
        StrayTypeParameterDocumentationRule,
        MissingReturnValueDocumentationRule,
        UnresolvedCrefRule
    ];
}
