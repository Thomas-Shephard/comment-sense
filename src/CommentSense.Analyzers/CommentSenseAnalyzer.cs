using System.Collections.Immutable;
using CommentSense.Analyzers.Logic;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentSenseAnalyzer : DiagnosticAnalyzer
{
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => CommentSenseRules.SupportedDiagnostics;

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();

        context.RegisterCompilationStartAction(compilationContext =>
        {
            var hasEnabled = false;
            var hasDisabled = false;

            foreach (var tree in compilationContext.Compilation.SyntaxTrees)
            {
                var isNone = tree.IsDocumentationModeNone();
                hasEnabled |= !isNone;
                hasDisabled |= isNone;

                if (hasEnabled && hasDisabled) break;
            }

            if (hasDisabled)
            {
                compilationContext.RegisterCompilationEndAction(ctx =>
                    ctx.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.DisabledDocumentationParsingRule, Location.None)));
            }

            if (!hasEnabled)
                return;

            compilationContext.RegisterSymbolAction(AnalyzeSymbol,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);

            compilationContext.RegisterSyntaxNodeAction(CrefAnalyzer.Analyze, SyntaxKind.XmlCrefAttribute);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        SyntaxTree? tree = (from location in symbol.Locations where !location.SourceTree.IsDocumentationModeNone() select location.SourceTree).FirstOrDefault();

        if (tree is null)
            return;

        AnalyzeSymbolCore(context, symbol, tree);
    }

    private static void AnalyzeSymbolCore(SymbolAnalysisContext context, ISymbol symbol, SyntaxTree tree)
    {
        var analyzeInternal = AnalyzerOptions.GetBoolOption(context.Options.AnalyzerConfigOptionsProvider, tree, "analyze_internal", defaultValue: false);
        var lowQualityTerms = AnalyzerOptions.GetStringListOption(context.Options.AnalyzerConfigOptionsProvider, tree, "low_quality_terms");
        var ignoredExceptions = AnalyzerOptions.GetStringListOption(context.Options.AnalyzerConfigOptionsProvider, tree, "ignored_exceptions");

        if (!symbol.IsEligibleForAnalysis(analyzeInternal))
            return;

        var xml = symbol.GetDocumentationCommentXml();

        if (!DocumentationExtensions.TryParseDocumentation(xml, out var element) || !DocumentationExtensions.HasValidDocumentation(element))
        {
            ReportMissingDocs(context, symbol);
            return;
        }

        if (DocumentationExtensions.HasAutoValidTag(element))
            return;

        SummaryAnalyzer.Analyze(context, symbol, element, lowQualityTerms);

        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                ParameterAnalyzer.Analyze(context, methodSymbol.Parameters, methodSymbol, element, lowQualityTerms);
                TypeParameterAnalyzer.Analyze(context, methodSymbol.TypeParameters, methodSymbol, element, lowQualityTerms);
                ReturnValueAnalyzer.Analyze(context, methodSymbol, element, lowQualityTerms);
                ExceptionAnalyzer.Analyze(context, methodSymbol, element, ignoredExceptions, lowQualityTerms, isPrimaryCtor: methodSymbol.IsPrimaryConstructor());
                break;
            case IPropertySymbol propertySymbol:
                if (propertySymbol.IsIndexer)
                {
                    ParameterAnalyzer.Analyze(context, propertySymbol.Parameters, propertySymbol, element, lowQualityTerms);
                }
                ReturnValueAnalyzer.Analyze(context, propertySymbol, element, lowQualityTerms);
                ExceptionAnalyzer.Analyze(context, propertySymbol, element, ignoredExceptions, lowQualityTerms);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                TypeParameterAnalyzer.Analyze(context, namedTypeSymbol.TypeParameters, namedTypeSymbol, element, lowQualityTerms);
                if (namedTypeSymbol is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null })
                {
                    ParameterAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod.Parameters, namedTypeSymbol, element, lowQualityTerms);
                    ReturnValueAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod, namedTypeSymbol, element, lowQualityTerms);
                }

                if (namedTypeSymbol.GetPrimaryConstructor() is { } primaryCtor)
                {
                    ParameterAnalyzer.Analyze(context, primaryCtor.Parameters, namedTypeSymbol, element, lowQualityTerms);
                    ReturnValueAnalyzer.Analyze(context, primaryCtor, namedTypeSymbol, element, lowQualityTerms);
                    ExceptionAnalyzer.Analyze(context, namedTypeSymbol, element, ignoredExceptions, lowQualityTerms, isPrimaryCtor: true);
                }
                break;
        }
    }

    private static void ReportMissingDocs(SymbolAnalysisContext context, ISymbol symbol)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingDocumentationRule, location, symbol.Name));
    }
}
