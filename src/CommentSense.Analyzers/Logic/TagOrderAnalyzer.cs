using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Analyzers.Logic;

internal static class TagOrderAnalyzer
{
    public static void Analyze(SymbolAnalysisContext context, DocumentationComment documentation, CommentSenseOptions options)
    {
        var topLevelElements = documentation.GetElements(recursive: false).ToList();
        if (topLevelElements.Count < 2)
            return;

        var tagOrder = options.TagOrder;
        var lastPriority = int.MinValue;
        XmlNodeSyntax? lastElement = null;

        foreach (var element in topLevelElements)
        {
            var tagName = element.GetTagName();
            if (!tagOrder.TryGetValue(tagName, out var currentPriority))
            {
                currentPriority = 100;
            }

            if (currentPriority < lastPriority && lastElement != null)
            {
                var location = element.GetLocation();
                context.ReportDiagnostic(Diagnostic.Create(
                    CommentSenseRules.DocumentationTagOrderMismatchRule,
                    location,
                    tagName,
                    lastElement.GetTagName()));
                return;
            }

            lastPriority = currentPriority;
            lastElement = element;
        }
    }
}
