using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using System.Collections.Concurrent;

namespace CommentSense.Analyzers.Logic;

internal static class DocumentationTextAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context, ConcurrentDictionary<XmlTextSyntax, bool> analyzedNodes)
    {
        var xmlText = (XmlTextSyntax)context.Node;

        // Ensure each XmlText node is only analyzed once per analysis pass.
        // This avoids duplicate diagnostics when the same documentation is associated with multiple symbols.
        if (!analyzedNodes.TryAdd(xmlText, true))
            return;

        var symbol = xmlText.GetAssociatedSymbol(context.SemanticModel);
        if (symbol is null)
            return;

        var tree = context.Node.SyntaxTree;
        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);

        if (!symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        LangwordAnalyzer.Analyze(context, xmlText, options);
        GhostReferenceAnalyzer.Analyze(context, xmlText, symbol, options);
    }
}
