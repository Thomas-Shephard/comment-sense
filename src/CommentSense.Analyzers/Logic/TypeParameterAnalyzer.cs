using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class TypeParameterAnalyzer
{
    private static readonly CollectionDocumentationAnalyzer.CollectionRuleSet Rules = new(
        DocumentationTags.TypeParam,
        CommentSenseRules.MissingTypeParameterDocumentationRule,
        CommentSenseRules.StrayTypeParameterDocumentationRule,
        CommentSenseRules.DuplicateTypeParameterDocumentationRule,
        CommentSenseRules.TypeParameterOrderMismatchRule);

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        CollectionDocumentationAnalyzer.Analyze(context, typeParameters, symbol, xml, options, Rules, topLevelOnly: true);
    }
}
