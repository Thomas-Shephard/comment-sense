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
        if (parameters.IsEmpty && !xml.Descendants("param").Any())
            return;

        var documentedParamNames = DocumentationExtensions.GetParamNames(xml).ToList();
        var documentedParamsSet = new HashSet<string>(documentedParamNames, StringComparer.Ordinal);
        var actualParamIndexMap = parameters.Select((p, i) => (p.Name, i)).ToDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);

        // CSENSE002: Missing Parameter Documentation
        foreach (var parameter in parameters)
        {
            if (documentedParamsSet.Contains(parameter.Name))
                continue;

            var location = parameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", parameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingParameterDocumentationRule, location, properties, parameter.Name));
        }

        var seenParams = new HashSet<string>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var documentedParam in documentedParamNames)
        {
            // CSENSE009: Duplicate Parameter Documentation
            if (!seenParams.Add(documentedParam))
            {
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.DuplicateParameterDocumentationRule, location, properties, documentedParam));
                continue;
            }

            if (!actualParamIndexMap.TryGetValue(documentedParam, out var currentIndex))
            {
                // CSENSE003: Stray Parameter Documentation
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayParameterDocumentationRule, location, properties, documentedParam));
                continue;
            }

            // CSENSE008: Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.ParameterOrderMismatchRule, location, properties, documentedParam));
            }

            lastActualIndex = currentIndex;
        }
    }
}
