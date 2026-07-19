using System.Collections.Concurrent;
using System.Collections.Immutable;
using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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

            var analyzedNodes = new ConcurrentDictionary<XmlTextSyntax, bool>();

            compilationContext.RegisterSymbolAction(AnalyzeSymbol,
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);

            compilationContext.RegisterSyntaxNodeAction(CrefAnalyzer.Analyze, SyntaxKind.XmlCrefAttribute);
            compilationContext.RegisterSyntaxNodeAction(c => DocumentationTextAnalyzer.Analyze(c, analyzedNodes), SyntaxKind.XmlText);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;
        SyntaxTree? tree = null;
        foreach (var location in symbol.Locations)
        {
            if (!location.SourceTree.IsDocumentationModeNone())
            {
                tree = location.SourceTree;
                break;
            }
        }

        if (tree is null)
            return;

        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);
        AnalyzeSymbolCore(context, symbol, options);
    }

    private static void AnalyzeSymbolCore(SymbolAnalysisContext context, ISymbol symbol, CommentSenseOptions options)
    {
        if (!IsEligibleForAnalysis(symbol, options))
            return;

        var documentation = DocumentationComment.FromSymbol(symbol, context.CancellationToken);
        if (documentation is null || documentation.IsMalformedFor(symbol, context.CancellationToken))
        {
            ReportMissingDocumentation(context, symbol, options);
            return;
        }

        TagOrderAnalyzer.Analyze(context, documentation, options);

        if (!documentation.HasValidDocumentation())
        {
            ReportMissingDocumentation(context, symbol, options);
            return;
        }

        if (InheritDocAnalyzer.Analyze(context, symbol, documentation))
            return;

        SummaryAnalyzer.Analyze(context, symbol, documentation, options);
        SupplementalAnalyzer.Analyze(context, symbol, documentation, options);
        AnalyzeSpecificSymbol(context, symbol, documentation, options);
    }

    private static bool IsEligibleForAnalysis(ISymbol symbol, CommentSenseOptions options)
    {
        if (!symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return false;

        if (options.ExcludeConstants && symbol is IFieldSymbol { IsConst: true })
            return false;

        return !(options.ExcludeEnums && symbol is IFieldSymbol { ContainingType.TypeKind: TypeKind.Enum });
    }

    private static void ReportMissingDocumentation(SymbolAnalysisContext context, ISymbol symbol, CommentSenseOptions options)
    {
        var isInheriting = symbol.IsInheriting();

        if (options.AllowImplicitInheritDoc && isInheriting && symbol.Kind != SymbolKind.NamedType)
            return;

        if (isInheriting && symbol.Kind != SymbolKind.NamedType)
        {
            ReportMissingInheritDoc(context, symbol);
            return;
        }

        ReportMissingDocs(context, symbol);
    }

    private static void AnalyzeSpecificSymbol(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                ParameterAnalyzer.Analyze(context, methodSymbol.Parameters, documentation, options);
                TypeParameterAnalyzer.Analyze(context, methodSymbol.TypeParameters, documentation, options);
                ReturnValueAnalyzer.Analyze(context, methodSymbol, documentation, options);
                ExceptionAnalyzer.Analyze(context, methodSymbol, documentation, options, isPrimaryCtor: methodSymbol.IsPrimaryConstructor());
                break;
            case IPropertySymbol propertySymbol:
                if (propertySymbol.IsIndexer)
                {
                    ParameterAnalyzer.Analyze(context, propertySymbol.Parameters, documentation, options);
                }
                ReturnValueAnalyzer.Analyze(context, propertySymbol, documentation, options);
                ExceptionAnalyzer.Analyze(context, propertySymbol, documentation, options);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                TypeParameterAnalyzer.Analyze(context, namedTypeSymbol.TypeParameters, documentation, options);
                if (namedTypeSymbol is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null })
                {
                    ParameterAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod.Parameters, documentation, options);
                    ReturnValueAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod, namedTypeSymbol, documentation, options);
                }

                if (namedTypeSymbol.GetPrimaryConstructor() is { } primaryCtor)
                {
                    ParameterAnalyzer.Analyze(context, primaryCtor.Parameters, documentation, options);
                    ReturnValueAnalyzer.Analyze(context, primaryCtor, namedTypeSymbol, documentation, options);
                    ExceptionAnalyzer.Analyze(context, namedTypeSymbol, documentation, options, isPrimaryCtor: true);
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
