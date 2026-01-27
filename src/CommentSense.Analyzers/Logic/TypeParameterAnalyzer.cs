using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TypeParameterAnalyzer
{
    private const string TypeParamTag = "typeparam";
    private const string NameProperty = "Name";

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ISymbol symbol, XElement xml, ImmutableHashSet<string> customLowQualityTerms)
    {
        if (typeParameters.IsEmpty && !xml.Descendants(TypeParamTag).Any())
            return;

        var documentedTypeParamNames = DocumentationExtensions.GetTypeParamNames(xml).ToList();
        var documentedTypeParamsSet = new HashSet<string>(documentedTypeParamNames, StringComparer.Ordinal);

        ReportMissingTypeParameters(context, typeParameters, documentedTypeParamsSet);

        var actualTypeParamIndexMap = new Dictionary<string, int>(typeParameters.Length, StringComparer.Ordinal);
        var actualTypeParamsByName = new Dictionary<string, ITypeParameterSymbol>(typeParameters.Length, StringComparer.Ordinal);
        for (int i = 0; i < typeParameters.Length; i++)
        {
            var p = typeParameters[i];
            actualTypeParamIndexMap[p.Name] = i;
            actualTypeParamsByName[p.Name] = p;
        }

        ValidateDocumentedTypeParameters(context, symbol, xml, actualTypeParamIndexMap, actualTypeParamsByName, customLowQualityTerms);
    }

    private static void ReportMissingTypeParameters(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, HashSet<string> documentedTypeParamsSet)
    {
        foreach (var typeParameter in typeParameters)
        {
            if (documentedTypeParamsSet.Contains(typeParameter.Name))
                continue;

            var location = typeParameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, typeParameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingTypeParameterDocumentationRule, location, properties, typeParameter.Name));
        }
    }

    private static void ValidateDocumentedTypeParameters(SymbolAnalysisContext context, ISymbol symbol, XElement xml, Dictionary<string, int> actualTypeParamIndexMap, Dictionary<string, ITypeParameterSymbol> actualTypeParamsByName, ImmutableHashSet<string> customLowQualityTerms)
    {
        var seenTypeParams = new HashSet<string>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var typeParamElement in DocumentationExtensions.GetTargetElements(xml, TypeParamTag))
        {
            var name = typeParamElement.Attribute("name")?.Value;
            if (name is null || string.IsNullOrWhiteSpace(name))
                continue;

            // CSENSE011: Duplicate Type Parameter Documentation
            if (!seenTypeParams.Add(name))
            {
                Report(context, symbol, CommentSenseRules.DuplicateTypeParameterDocumentationRule, name);
                continue;
            }

            // CSENSE005: Stray Type Parameter Documentation
            if (!actualTypeParamIndexMap.TryGetValue(name, out var currentIndex))
            {
                Report(context, symbol, CommentSenseRules.StrayTypeParameterDocumentationRule, name);
                continue;
            }

            // CSENSE016: Low Quality Type Parameter Documentation
            if (QualityAnalyzer.IsLowQuality(typeParamElement, name, customLowQualityTerms))
            {
                var location = actualTypeParamsByName[name].Locations.GetPrimaryLocation();
                QualityAnalyzer.Report(context, location, TypeParamTag, name);
            }

            // CSENSE010: Type Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                Report(context, symbol, CommentSenseRules.TypeParameterOrderMismatchRule, name);
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
