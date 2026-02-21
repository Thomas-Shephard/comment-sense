using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TagOrderAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        var topLevelElements = DocumentationXmlExtensions.GetTargetElements(xml).ToList();
        if (topLevelElements.Count < 2)
            return;

        var tagOrder = options.TagOrder;
        var lastPriority = int.MinValue;
        XElement? lastElement = null;

        foreach (var element in topLevelElements)
        {
            var tagName = element.Name.LocalName;
            if (!tagOrder.TryGetValue(tagName, out var currentPriority))
            {
                // Unknown tags are treated as having the lowest priority (highest number)
                currentPriority = 100;
            }

            if (currentPriority < lastPriority && lastElement != null)
            {
                var occurrence = GetOccurrence(topLevelElements, element);
                var location = locationCache.GetLocation(symbol, tagName, topLevelOnly: true, occurrence: occurrence);
                context.ReportDiagnostic(Diagnostic.Create(
                    CommentSenseRules.DocumentationTagOrderMismatchRule,
                    location,
                    tagName,
                    lastElement.Name.LocalName));
                return; // Report only the first out-of-order tag to avoid noise
            }

            lastPriority = currentPriority;
            lastElement = element;
        }
    }

    internal static int GetOccurrence(List<XElement> elements, XElement target)
    {
        int occurrence = 0;
        var tagName = target.Name.LocalName;
        foreach (var element in elements)
        {
            if (element == target)
                return occurrence;

            if (element.Name.LocalName == tagName)
                occurrence++;
        }

        return occurrence;
    }
}
