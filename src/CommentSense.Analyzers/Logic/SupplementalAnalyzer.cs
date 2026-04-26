using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SupplementalAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        var displayName = symbol.GetDisplayName();
        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        AnalyzeTag(context, documentation, options, DocumentationTags.Remarks, displayName, minimallyQualifiedName);
        AnalyzeTag(context, documentation, options, DocumentationTags.Example, displayName, minimallyQualifiedName);
    }

    private static void AnalyzeTag(SymbolAnalysisContext context, DocumentationComment documentation, CommentSenseOptions options, string tagName, string displayName, string minimallyQualifiedName)
    {
        foreach (var element in documentation.GetElements(tagName, recursive: false))
        {
            var location = element.GetLocation();
            if (QualityAnalyzer.IsLowQualityForAnyFormat(element, displayName, minimallyQualifiedName, options, tagName))
                QualityAnalyzer.Report(context, location, tagName, displayName);
        }
    }
}
