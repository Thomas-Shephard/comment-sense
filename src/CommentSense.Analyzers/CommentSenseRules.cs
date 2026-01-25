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
        "The parameter '{0}' in the documentation does not exist in the signature",
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
        "The symbol '{0}' is missing return value documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Publicly accessible non-void members should have a <returns> tag to document the return value.");

    public static readonly DiagnosticDescriptor UnresolvedCrefRule = new(
        CommentSenseDiagnosticIds.UnresolvedCrefId,
        "Invalid XML documentation reference",
        "The cref reference '{0}' could not be resolved",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The 'cref' attribute in XML documentation must refer to a valid symbol.");

    public static readonly DiagnosticDescriptor ParameterOrderMismatchRule = new(
        CommentSenseDiagnosticIds.ParameterOrderMismatchId,
        "Parameter documentation order mismatch",
        "The parameter '{0}' is documented out of order",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The order of <param> tags should match the order of parameters in the member signature.");

    public static readonly DiagnosticDescriptor DuplicateParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.DuplicateParameterDocumentationId,
        "Duplicate parameter documentation",
        "The parameter '{0}' is documented more than once",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each parameter should only be documented once.");

    public static readonly DiagnosticDescriptor TypeParameterOrderMismatchRule = new(
        CommentSenseDiagnosticIds.TypeParameterOrderMismatchId,
        "Type parameter documentation order mismatch",
        "The type parameter '{0}' is documented out of order",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The order of <typeparam> tags should match the order of type parameters in the member signature.");

    public static readonly DiagnosticDescriptor DuplicateTypeParameterDocumentationRule = new(
        CommentSenseDiagnosticIds.DuplicateTypeParameterDocumentationId,
        "Duplicate type parameter documentation",
        "The type parameter '{0}' is documented more than once",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Each type parameter should only be documented once.");

    public static readonly DiagnosticDescriptor MissingExceptionDocumentationRule = new(
        CommentSenseDiagnosticIds.MissingExceptionDocumentationId,
        "Missing exception documentation",
        "The exception type '{0}' is thrown but not documented",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All exceptions explicitly thrown by a publicly accessible member should be documented with an <exception> tag.");

    public static readonly DiagnosticDescriptor StrayReturnValueDocumentationRule = new(
        CommentSenseDiagnosticIds.StrayReturnValueDocumentationId,
        "Stray return value documentation",
        "The symbol '{0}' should not have return value documentation as it returns void or a non-generic Task",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Documentation should not contain <returns> tags for members that do not return a value.");

    public static readonly DiagnosticDescriptor InvalidExceptionCrefRule = new(
        CommentSenseDiagnosticIds.InvalidExceptionCrefId,
        "Invalid exception type in documentation",
        "The type '{0}' documented in an <exception> tag is not an exception type",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "The 'cref' attribute in an <exception> tag should refer to a type that inherits from System.Exception.");

    public static readonly ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics = [
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
        InvalidExceptionCrefRule
    ];
}
