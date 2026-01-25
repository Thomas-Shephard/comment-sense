using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ReturnValueAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, IMethodSymbol method, XElement xml)
    {
        Analyze(context, method, method, xml);
    }

    public static void Analyze(SymbolAnalysisContext context, IMethodSymbol method, ISymbol reportSymbol, XElement xml)
    {
        var location = reportSymbol.Locations.GetPrimaryLocation();
        var symbolName = reportSymbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Methods should not have <value> tag
        if (DocumentationExtensions.HasValueTag(xml))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayValueDocumentationRule, location, symbolName));
        }

        bool effectivelyReturnsVoid = method.ReturnsVoid || IsTaskOrValueTask(method.ReturnType);
        bool hasReturnsTag = DocumentationExtensions.HasReturnsTag(xml);

        if (effectivelyReturnsVoid)
        {
            if (hasReturnsTag)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, symbolName));
            }
        }
        else if (!hasReturnsTag)
        {
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "returns");
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
        }
    }

    public static void Analyze(SymbolAnalysisContext context, IPropertySymbol property, XElement xml)
    {
        var location = property.Locations.GetPrimaryLocation();
        var symbolName = property.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Properties and indexers should not have <returns> tag
        if (DocumentationExtensions.HasReturnsTag(xml))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, symbolName));
        }

        bool hasValueTag = DocumentationExtensions.HasValueTag(xml);

        if (property.GetMethod != null && !hasValueTag)
        {
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "value");
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingValueDocumentationRule, location, properties, symbolName));
        }
    }

    private static bool IsTaskOrValueTask(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { Arity: 0 } namedType)
            return false;

        if (namedType.Name != "Task" && namedType.Name != "ValueTask")
            return false;

        var ns = namedType.ContainingNamespace.ToDisplayString();
        return ns == "System.Threading.Tasks";
    }
}
