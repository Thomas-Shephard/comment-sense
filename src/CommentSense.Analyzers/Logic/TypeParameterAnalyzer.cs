using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TypeParameterAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ImmutableArray<Location> symbolLocations, XElement xml)
    {
        var documentedTypeParams = new HashSet<string>(DocumentationExtensions.GetTypeParamNames(xml), StringComparer.Ordinal);
        var actualTypeParams = new HashSet<string>(StringComparer.Ordinal);

        // CSENSE004: Missing Type Parameter Documentation
        foreach (var typeParameter in typeParameters)
        {
            actualTypeParams.Add(typeParameter.Name);

            if (documentedTypeParams.Contains(typeParameter.Name))
                continue;

            var location = typeParameter.Locations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", typeParameter.Name);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingTypeParameterDocumentationRule, location, properties, typeParameter.Name));
        }

        // CSENSE005: Stray Type Parameter Documentation
        foreach (var documentedTypeParam in documentedTypeParams.Where(p => !actualTypeParams.Contains(p)))
        {
            var location = symbolLocations.GetPrimaryLocation();
            var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", documentedTypeParam);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayTypeParameterDocumentationRule, location, properties, documentedTypeParam));
        }
    }
}
