using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TypeParameterAnalyzer
{
    private const string TypeParamTag = "typeparam";
    private const string NameProperty = "Name";

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        if (typeParameters.IsEmpty && !xml.Descendants(TypeParamTag).Any())
            return;

        var documentedTypeParamNames = DocumentationExtensions.GetTypeParamNames(xml).ToList();
        var documentedTypeParamsSet = new HashSet<string>(documentedTypeParamNames, StringComparer.Ordinal);

        if (!DocumentationExtensions.HasInheritDoc(xml) && !DocumentationExtensions.HasAutoValidTag(xml))
            ReportMissingTypeParameters(context, typeParameters, documentedTypeParamsSet);

        var actualTypeParamIndexMap = new Dictionary<string, int>(typeParameters.Length, StringComparer.Ordinal);
        for (int i = 0; i < typeParameters.Length; i++)
        {
            var p = typeParameters[i];
            actualTypeParamIndexMap[p.Name] = i;
        }

        ValidateDocumentedTypeParameters(context, symbol, xml, actualTypeParamIndexMap, options);
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

    private static void ValidateDocumentedTypeParameters(SymbolAnalysisContext context, ISymbol symbol, XElement xml, Dictionary<string, int> actualTypeParamIndexMap, CommentSenseOptions options)
    {
        var seenTypeParams = new Dictionary<string, int>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        var typeParamElements = DocumentationExtensions.GetTargetElements(xml, TypeParamTag).ToList();
        var typeParamLocations = symbol.GetDocumentationLocations(TypeParamTag, topLevelOnly: false);

        for (int i = 0; i < typeParamElements.Count; i++)
        {
            var typeParamElement = typeParamElements[i];
            var name = typeParamElement.Attribute("name")?.Value;
            if (name is null || string.IsNullOrWhiteSpace(name))
                continue;

            if (!seenTypeParams.TryGetValue(name, out var occurrence))
                occurrence = 0;

            seenTypeParams[name] = occurrence + 1;

            var location = typeParamLocations.GetLocationOrDefault(i, symbol);

            // CSENSE011: Duplicate Type Parameter Documentation
            if (occurrence > 0)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.DuplicateTypeParameterDocumentationRule, location, properties, name));
                continue;
            }

            // CSENSE005: Stray Type Parameter Documentation
            if (!actualTypeParamIndexMap.TryGetValue(name, out var currentIndex))
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayTypeParameterDocumentationRule, location, properties, name));
                continue;
            }

            // CSENSE016: Low Quality Type Parameter Documentation
            if (QualityAnalyzer.IsLowQuality(typeParamElement, name, options, tagName: TypeParamTag))
            {
                QualityAnalyzer.Report(context, location, TypeParamTag, name);
            }

            // CSENSE010: Type Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                var properties = ImmutableDictionary<string, string?>.Empty.Add(NameProperty, name);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.TypeParameterOrderMismatchRule, location, properties, name));
            }

            lastActualIndex = currentIndex;
        }
    }
}
