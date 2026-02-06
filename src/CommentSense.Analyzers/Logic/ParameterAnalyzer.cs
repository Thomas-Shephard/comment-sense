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

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        if (parameters.IsEmpty && !xml.Descendants(ParamTag).Any())
            return;

        var documentedParamNames = DocumentationExtensions.GetParamNames(xml).ToList();
        var documentedParamsSet = new HashSet<string>(documentedParamNames, StringComparer.Ordinal);

        if (!DocumentationExtensions.HasInheritDoc(xml) && !DocumentationExtensions.HasAutoValidTag(xml))
            ReportMissingParameters(context, parameters, documentedParamsSet);

        var actualParamIndexMap = new Dictionary<string, int>(parameters.Length, StringComparer.Ordinal);
        for (int i = 0; i < parameters.Length; i++)
        {
            var p = parameters[i];
            actualParamIndexMap[p.Name] = i;
        }

        ValidateDocumentedParameters(context, symbol, xml, actualParamIndexMap, options);
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

    private static void ValidateDocumentedParameters(SymbolAnalysisContext context, ISymbol symbol, XElement xml, Dictionary<string, int> actualParamIndexMap, CommentSenseOptions options)
    {
        var seenParams = new Dictionary<string, int>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        var paramElements = DocumentationExtensions.GetTargetElements(xml, ParamTag).ToList();
        var paramLocations = symbol.GetDocumentationLocations(ParamTag);

        for (int i = 0; i < paramElements.Count; i++)
        {
            var paramElement = paramElements[i];
            var name = paramElement.Attribute("name")?.Value;
            if (name is null || string.IsNullOrWhiteSpace(name))
                continue;

            if (!seenParams.TryGetValue(name, out var occurrence))
                occurrence = 0;

            seenParams[name] = occurrence + 1;

            var location = paramLocations.GetLocationOrDefault(i, symbol);

            // CSENSE009: Duplicate Parameter Documentation
            if (occurrence > 0)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.DuplicateParameterDocumentationRule, location, properties, name));
                continue;
            }

            // CSENSE003: Stray Parameter Documentation
            if (!actualParamIndexMap.TryGetValue(name, out var currentIndex))
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayParameterDocumentationRule, location, properties, name));
                continue;
            }

            // CSENSE016: Low Quality Parameter Documentation
            if (QualityAnalyzer.IsLowQuality(paramElement, name, options, tagName: ParamTag))
            {
                QualityAnalyzer.Report(context, location, ParamTag, name);
            }

            // CSENSE008: Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.ParameterOrderMismatchRule, location, properties, name));
            }

            lastActualIndex = currentIndex;
        }
    }
}
