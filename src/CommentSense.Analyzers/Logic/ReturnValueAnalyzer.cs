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
        if (method.MethodKind != MethodKind.Ordinary && method.MethodKind != MethodKind.UserDefinedOperator && method.MethodKind != MethodKind.Conversion)
            return;

        if (method.ReturnsVoid)
            return;

        if (IsTaskOrValueTask(method.ReturnType))
            return;

        if (DocumentationExtensions.HasReturnsTag(xml))
            return;

        var location = method.Locations.GetPrimaryLocation();
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "returns");
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingReturnValueDocumentationRule, location, properties, method.Name));
    }

    private static bool IsTaskOrValueTask(ITypeSymbol typeSymbol)
    {
        if (typeSymbol is not INamedTypeSymbol { Arity: 0 } namedType)
            return false;

        return (namedType.Name == "Task" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks") ||
               (namedType.Name == "ValueTask" && namedType.ContainingNamespace.ToDisplayString() == "System.Threading.Tasks");
    }
}
