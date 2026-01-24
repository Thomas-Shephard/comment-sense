using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TypeParameterAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ISymbol symbol, XElement xml)
    {
        if (typeParameters.IsEmpty && !xml.Descendants("typeparam").Any())
            return;

        var documentedTypeParamNames = DocumentationExtensions.GetTypeParamNames(xml).ToList();
        var documentedTypeParamsSet = new HashSet<string>(documentedTypeParamNames, StringComparer.Ordinal);
        var actualTypeParamIndexMap = typeParameters.Select((p, i) => (p.Name, i)).ToDictionary(x => x.Name, x => x.i, StringComparer.Ordinal);

        // CSENSE004: Missing Type Parameter Documentation
        foreach (var typeParameter in typeParameters)
        {
            if (documentedTypeParamsSet.Contains(typeParameter.Name))
                continue;

            var location = typeParameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", typeParameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingTypeParameterDocumentationRule, location, properties, typeParameter.Name));
        }

        var seenTypeParams = new HashSet<string>(StringComparer.Ordinal);
        var lastActualIndex = -1;

        foreach (var documentedTypeParam in documentedTypeParamNames)
        {
            // CSENSE011: Duplicate Type Parameter Documentation
            if (!seenTypeParams.Add(documentedTypeParam))
            {
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedTypeParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.DuplicateTypeParameterDocumentationRule, location, properties, documentedTypeParam));
                continue;
            }

            if (!actualTypeParamIndexMap.TryGetValue(documentedTypeParam, out var currentIndex))
            {
                // CSENSE005: Stray Type Parameter Documentation
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedTypeParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayTypeParameterDocumentationRule, location, properties, documentedTypeParam));
                continue;
            }

            // CSENSE010: Type Parameter Order Mismatch
            if (currentIndex < lastActualIndex)
            {
                var location = symbol.Locations.GetPrimaryLocation();
                var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedTypeParam);
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.TypeParameterOrderMismatchRule, location, properties, documentedTypeParam));
            }

            lastActualIndex = currentIndex;
        }
    }
}
