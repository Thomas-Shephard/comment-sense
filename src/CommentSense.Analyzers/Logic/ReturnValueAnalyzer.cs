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
        if (method.MethodKind != MethodKind.Ordinary &&
            method.MethodKind != MethodKind.UserDefinedOperator &&
            method.MethodKind != MethodKind.Conversion &&
            method.MethodKind != MethodKind.DelegateInvoke)
            return;

        AnalyzeInternal(context, reportSymbol, method.ReturnType, method.ReturnsVoid, xml);
    }

    public static void Analyze(SymbolAnalysisContext context, IPropertySymbol property, XElement xml)
    {
        if (!property.IsIndexer || property.GetMethod == null)
            return;

        AnalyzeInternal(context, property, property.Type, false, xml);
    }

    private static void AnalyzeInternal(SymbolAnalysisContext context, ISymbol symbol, ITypeSymbol returnType, bool returnsVoid, XElement xml)
    {
        bool effectivelyReturnsVoid = returnsVoid || IsTaskOrValueTask(returnType);
        bool hasReturnsTag = DocumentationExtensions.HasReturnsTag(xml);
        var location = symbol.Locations.GetPrimaryLocation();
        var symbolName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        if (effectivelyReturnsVoid)
        {
            if (hasReturnsTag)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayReturnValueDocumentationRule, location, symbolName));
            }

            return;
        }

        if (hasReturnsTag)
            return;

        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "returns");
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, symbolName));
    }

    private static bool IsTaskOrValueTask(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { Arity: 0 } namedType)
            return false;

        return (namedType.Name == "Task" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks") ||
               (namedType.Name == "ValueTask" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks");
    }
}
