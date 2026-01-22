using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentSenseAnalyzer : DiagnosticAnalyzer
{
    private const string MissingDocumentationId = "CSENSE001";
    private const string MissingParameterDocumentationId = "CSENSE002";
    private const string StrayParameterDocumentationId = "CSENSE003";
    private const string MissingTypeParameterDocumentationId = "CSENSE004";
    private const string StrayTypeParameterDocumentationId = "CSENSE005";
    private const string MissingReturnValueDocumentationId = "CSENSE006";

    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor MissingDocumentationRule = new(
        MissingDocumentationId,
        "Public API is missing documentation",
        "The symbol '{0}' is missing valid documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Publicly accessible APIs should be documented to ensure maintainability and clarity.");

    private static readonly DiagnosticDescriptor MissingParameterDocumentationRule = new(
        MissingParameterDocumentationId,
        "Missing parameter documentation",
        "The parameter '{0}' is missing documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All parameters of a publicly accessible member should be documented.");

    private static readonly DiagnosticDescriptor StrayParameterDocumentationRule = new(
        StrayParameterDocumentationId,
        "Stray parameter documentation",
        "The parameter '{0}' in the documentation does not exist in the method signature",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Documentation should not contain <param> tags for parameters that do not exist.");

    private static readonly DiagnosticDescriptor MissingTypeParameterDocumentationRule = new(
        MissingTypeParameterDocumentationId,
        "Missing type parameter documentation",
        "The type parameter '{0}' is missing documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "All type parameters of a publicly accessible member should be documented.");

    private static readonly DiagnosticDescriptor StrayTypeParameterDocumentationRule = new(
        StrayTypeParameterDocumentationId,
        "Stray type parameter documentation",
        "The type parameter '{0}' in the documentation does not exist in the signature",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Documentation should not contain <typeparam> tags for type parameters that do not exist.");

    private static readonly DiagnosticDescriptor MissingReturnValueDocumentationRule = new(
        MissingReturnValueDocumentationId,
        "Missing return value documentation",
        "The method '{0}' is missing return value documentation",
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Non-void methods of a publicly accessible member should have a <returns> tag to document the return value.");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
        MissingDocumentationRule,
        MissingParameterDocumentationRule,
        StrayParameterDocumentationRule,
        MissingTypeParameterDocumentationRule,
        StrayTypeParameterDocumentationRule,
        MissingReturnValueDocumentationRule
    ];

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterSymbolAction(AnalyzeSymbol,
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Field,
            SymbolKind.Event);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;

        if (!AnalysisEngine.IsEligibleForAnalysis(symbol))
            return;

        var xml = symbol.GetDocumentationCommentXml();

        if (!DocumentationExtensions.TryParseDocumentation(xml, out var element) || !DocumentationExtensions.HasValidDocumentation(element))
        {
            ReportMissingDocs(context, symbol);
            return;
        }

        if (DocumentationExtensions.HasAutoValidTag(element))
            return;

        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                AnalyzeMethodParameters(context, methodSymbol, element);
                AnalyzeTypeParameters(context, methodSymbol.TypeParameters, methodSymbol.Locations, element);
                AnalyzeReturnValue(context, methodSymbol, element);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                AnalyzeTypeParameters(context, namedTypeSymbol.TypeParameters, namedTypeSymbol.Locations, element);
                break;
        }
    }

    private static void ReportMissingDocs(SymbolAnalysisContext context, ISymbol symbol)
    {
        var location = AnalysisEngine.GetPrimaryLocation(symbol.Locations);
        context.ReportDiagnostic(Diagnostic.Create(MissingDocumentationRule, location, symbol.Name));
    }

    private static void AnalyzeTypeParameters(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ImmutableArray<Location> symbolLocations, XElement xml)
    {
        var documentedTypeParams = new HashSet<string>(DocumentationExtensions.GetTypeParamNames(xml), StringComparer.Ordinal);
        var actualTypeParams = new HashSet<string>(StringComparer.Ordinal);

        // CSENSE004: Missing Type Parameter Documentation
        foreach (var typeParameter in typeParameters)
        {
            actualTypeParams.Add(typeParameter.Name);

            if (documentedTypeParams.Contains(typeParameter.Name))
                continue;

            var location = AnalysisEngine.GetPrimaryLocation(typeParameter.Locations);
            context.ReportDiagnostic(Diagnostic.Create(MissingTypeParameterDocumentationRule, location, typeParameter.Name));
        }

        // CSENSE005: Stray Type Parameter Documentation
        foreach (var documentedTypeParam in documentedTypeParams.Where(p => !actualTypeParams.Contains(p)))
        {
            var location = AnalysisEngine.GetPrimaryLocation(symbolLocations);
            context.ReportDiagnostic(Diagnostic.Create(StrayTypeParameterDocumentationRule, location, documentedTypeParam));
        }
    }

    private static void AnalyzeMethodParameters(SymbolAnalysisContext context, IMethodSymbol method, XElement xml)
    {
        var documentedParams = new HashSet<string>(DocumentationExtensions.GetParamNames(xml), StringComparer.Ordinal);
        var actualParams = new HashSet<string>(StringComparer.Ordinal);

        // CSENSE002: Missing Parameter Documentation
        foreach (var parameter in method.Parameters)
        {
            actualParams.Add(parameter.Name);

            if (documentedParams.Contains(parameter.Name))
                continue;

            var location = AnalysisEngine.GetPrimaryLocation(parameter.Locations);
            context.ReportDiagnostic(Diagnostic.Create(MissingParameterDocumentationRule, location, parameter.Name));
        }

        // CSENSE003: Stray Parameter Documentation
        foreach (var documentedParam in documentedParams.Where(p => !actualParams.Contains(p)))
        {
            var location = AnalysisEngine.GetPrimaryLocation(method.Locations);
            context.ReportDiagnostic(Diagnostic.Create(StrayParameterDocumentationRule, location, documentedParam));
        }
    }

    private static void AnalyzeReturnValue(SymbolAnalysisContext context, IMethodSymbol method, XElement xml)
    {
        if (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.UserDefinedOperator && method.MethodKind != MethodKind.Conversion)
            return;

        if (method.ReturnsVoid)
            return;

        if (IsTaskOrValueTask(method.ReturnType))
            return;

        if (DocumentationExtensions.HasReturnsTag(xml))
            return;

        var location = AnalysisEngine.GetPrimaryLocation(method.Locations);
        context.ReportDiagnostic(Diagnostic.Create(MissingReturnValueDocumentationRule, location, method.Name));
    }

    private static bool IsTaskOrValueTask(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { Arity: 0 } namedType)
            return false;

        return (namedType.Name == "Task" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks") ||
               (namedType.Name == "ValueTask" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks");
    }
}
