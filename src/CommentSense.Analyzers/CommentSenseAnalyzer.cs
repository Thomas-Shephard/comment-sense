using System.Collections.Immutable;
using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

/// <summary>
/// The main analyzer for CommentSense that enforces documentation quality rules.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentSenseAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc />
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => CommentSenseRules.SupportedDiagnostics;

    /// <inheritdoc />
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
            compilationContext.RegisterSyntaxNodeAction(DocumentationTextAnalyzer.Analyze, SyntaxKind.XmlText);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        SyntaxTree? tree = (from location in symbol.Locations where !location.SourceTree.IsDocumentationModeNone() select location.SourceTree).FirstOrDefault();

        if (tree is null)
            return;

        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);
        AnalyzeSymbolCore(context, symbol, options);
    }

    private static void AnalyzeSymbolCore(SymbolAnalysisContext context, ISymbol symbol, CommentSenseOptions options)
    {
        if (!symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        if (options.ExcludeConstants && symbol is IFieldSymbol { IsConst: true })
            return;

        if (options.ExcludeEnums && symbol is IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum })
            return;

        var isInheriting = symbol.IsInheriting();
        var xml = symbol.GetDocumentationCommentXml();

        if (!DocumentationExtensions.TryParseDocumentation(xml, out var element) || !DocumentationExtensions.HasValidDocumentation(element))
        {
            if (options.AllowImplicitInheritDoc && isInheriting && symbol.Kind != SymbolKind.NamedType)
                return;

            if (isInheriting && symbol.Kind != SymbolKind.NamedType)
            {
                ReportMissingInheritDoc(context, symbol);
                return;
            }

            ReportMissingDocs(context, symbol);
            return;
        }

        if (DocumentationExtensions.HasInheritDoc(element) &&
            !DocumentationExtensions.HasInheritDocWithCref(element) &&
            !isInheriting)
        {
            ReportMissingDocs(context, symbol);
            return;
        }

        SummaryAnalyzer.Analyze(context, symbol, element, options);
        AnalyzeSpecificSymbol(context, symbol, element, options);
    }

    private static void AnalyzeSpecificSymbol(SymbolAnalysisContext context, ISymbol symbol, System.Xml.Linq.XElement element, CommentSenseOptions options)
    {
        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                ParameterAnalyzer.Analyze(context, methodSymbol.Parameters, methodSymbol, element, options);
                TypeParameterAnalyzer.Analyze(context, methodSymbol.TypeParameters, methodSymbol, element, options);
                ReturnValueAnalyzer.Analyze(context, methodSymbol, element, options);
                ExceptionAnalyzer.Analyze(context, methodSymbol, element, options, isPrimaryCtor: methodSymbol.IsPrimaryConstructor());
                break;
            case IPropertySymbol propertySymbol:
                if (propertySymbol.IsIndexer)
                {
                    ParameterAnalyzer.Analyze(context, propertySymbol.Parameters, propertySymbol, element, options);
                }
                ReturnValueAnalyzer.Analyze(context, propertySymbol, element, options);
                ExceptionAnalyzer.Analyze(context, propertySymbol, element, options);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                TypeParameterAnalyzer.Analyze(context, namedTypeSymbol.TypeParameters, namedTypeSymbol, element, options);
                if (namedTypeSymbol is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null })
                {
                    ParameterAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod.Parameters, namedTypeSymbol, element, options);
                    ReturnValueAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod, namedTypeSymbol, element, options);
                }

                if (namedTypeSymbol.GetPrimaryConstructor() is { } primaryCtor)
                {
                    ParameterAnalyzer.Analyze(context, primaryCtor.Parameters, namedTypeSymbol, element, options);
                    ReturnValueAnalyzer.Analyze(context, primaryCtor, namedTypeSymbol, element, options);
                    ExceptionAnalyzer.Analyze(context, namedTypeSymbol, element, options, isPrimaryCtor: true);
                }
                break;
        }
    }

    private static void ReportMissingDocs(SymbolAnalysisContext context, ISymbol symbol)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingDocumentationRule, location, symbol.GetDisplayName()));
    }

    private static void ReportMissingInheritDoc(SymbolAnalysisContext context, ISymbol symbol)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingInheritDocRule, location, symbol.GetDisplayName()));
    }
}
