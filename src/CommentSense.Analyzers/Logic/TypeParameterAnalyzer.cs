using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.Core.Utilities;
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

    public static void Analyze(SymbolAnalysisContext context, ImmutableArray<ITypeParameterSymbol> typeParameters, DocumentationComment documentation, CommentSenseOptions options)
    {
        CollectionDocumentationAnalyzer.Analyze(context, typeParameters, documentation, options, Rules);
    }
}
