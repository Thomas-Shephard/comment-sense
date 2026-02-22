using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class SummaryAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        var seenSummary = false;
        var effectiveTarget = DocumentationXmlExtensions.GetEffectiveTarget(xml);
        var displayName = symbol.GetDisplayName();
        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        // Check for low-quality documentation against multiple symbol formats (e.g., friendly name and qualified name)
        foreach (var (summaryElement, location) in symbol.GetTargetElementsWithLocations(xml, DocumentationTags.Summary, topLevelOnly: false))
        {
            bool isTopLevel = DocumentationXmlExtensions.IsTopLevel(xml, summaryElement, effectiveTarget);

            if (!isTopLevel || seenSummary)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StraySummaryDocumentationRule, location, displayName));
                continue;
            }

            seenSummary = true;

            if (!QualityAnalyzer.IsLowQualityForAnyFormat(summaryElement, displayName, minimallyQualifiedName, options, DocumentationTags.Summary))
                continue;

            QualityAnalyzer.Report(context, location, DocumentationTags.Summary, displayName);
        }
    }
}
