using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ParameterAnalyzer
{
    private const string ParamTag = "param";
    private const string NameProperty = "Name";

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, ISymbol symbol, XElement xml)
    {
        if (parameters.IsEmpty && !xml.Descendants(ParamTag).Any())
            return;

        var documentedParamNames = DocumentationExtensions.GetParamNames(xml).ToList();
        var documentedParamsSet = new HashSet<string>(documentedParamNames, StringComparer.Ordinal);

        ReportMissingParameters(context, parameters, documentedParamsSet);

        var actualParamIndexMap = new Dictionary<string, int>(parameters.Length, StringComparer.Ordinal);
        var actualParamsByName = new Dictionary<string, IParameterSymbol>(parameters.Length, StringComparer.Ordinal);
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            actualParamIndexMap[p.Name] = i;
            actualParamsByName[p.Name] = p;
        }

        ValidateDocumentedParameters(context, symbol, xml, actualParamIndexMap, actualParamsByName);
    }

    private static void ReportMissingParameters(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, HashSet<string> documentedParamsSet)
    {
        foreach (var parameter in parameters)
        {
            if (documentedParamsSet.Contains(parameter.Name))
                continue;

            var location = parameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, parameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingParameterDocumentationRule, location, properties, parameter.Name));
        }
    }

    private static void ValidateDocumentedParameters(SymbolAnalysisContext context, ISymbol symbol, XElement xml, Dictionary<string, int> actualParamIndexMap, Dictionary<string, IParameterSymbol> actualParamsByName)
    {
        var seenParams = new HashSet<string>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var paramElement in DocumentationExtensions.GetTargetElements(xml, ParamTag))
        {
            var name = paramElement.Attribute("name")?.Value;
            if (name is null || string.IsNullOrWhiteSpace(name))
                continue;

            // CSENSE009: Duplicate Parameter Documentation
            if (!seenParams.Add(name))
            {
                Report(context, symbol, CommentSenseRules.DuplicateParameterDocumentationRule, name);
                continue;
            }

            // CSENSE003: Stray Parameter Documentation
            if (!actualParamIndexMap.TryGetValue(name, out var currentIndex))
            {
                Report(context, symbol, CommentSenseRules.StrayParameterDocumentationRule, name);
                continue;
            }

            // CSENSE016: Low Quality Parameter Documentation
            if (QualityAnalyzer.IsLowQuality(paramElement, name))
            {
                var location = actualParamsByName[name].Locations.GetPrimaryLocation();
                QualityAnalyzer.Report(context, location, ParamTag, name);
            }

            // CSENSE008: Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                Report(context, symbol, CommentSenseRules.ParameterOrderMismatchRule, name);
            }

            lastActualIndex = currentIndex;
        }
    }

    private static void Report(SymbolAnalysisContext context, ISymbol symbol, DiagnosticDescriptor rule, string name)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
        context.ReportDiagnostic(Diagnostic.Create(rule, location, properties, name));
    }
}
