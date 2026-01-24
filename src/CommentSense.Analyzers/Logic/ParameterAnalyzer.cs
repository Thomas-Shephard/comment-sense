using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ParameterAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, ISymbol symbol, XElement xml)
    {
        var documentedParams = new HashSet<string>(DocumentationExtensions.GetParamNames(xml), StringComparer.Ordinal);
        var actualParams = new HashSet<string>(StringComparer.Ordinal);

        // CSENSE002: Missing Parameter Documentation
        foreach (var parameter in parameters)
        {
            actualParams.Add(parameter.Name);

            if (documentedParams.Contains(parameter.Name))
                continue;

            var location = parameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", parameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingParameterDocumentationRule, location, properties, parameter.Name));
        }

        // CSENSE003: Stray Parameter Documentation
        foreach (var documentedParam in documentedParams.Where(p => !actualParams.Contains(p)))
        {
            var location = symbol.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedParam);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayParameterDocumentationRule, location, properties, documentedParam));
        }
    }
}
