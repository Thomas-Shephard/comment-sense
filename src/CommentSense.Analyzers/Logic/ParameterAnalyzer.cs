using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ParameterAnalyzer
{
    private static readonly CollectionDocumentationAnalyzer.CollectionRuleSet Rules = new(
        DocumentationTags.Param,
        CommentSenseRules.MissingParameterDocumentationRule,
        CommentSenseRules.StrayParameterDocumentationRule,
        CommentSenseRules.DuplicateParameterDocumentationRule,
        CommentSenseRules.ParameterOrderMismatchRule);

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, DocumentationComment documentation, CommentSenseOptions options)
    {
        CollectionDocumentationAnalyzer.Analyze(context, parameters, documentation, options, Rules);
    }
}
