using System.Collections.Immutable;
using System.Xml.Linq;
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

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<IParameterSymbol> parameters, ISymbol symbol, XElement xml, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        CollectionDocumentationAnalyzer.Analyze(context, parameters, symbol, xml, options, Rules, locationCache);
    }
}
