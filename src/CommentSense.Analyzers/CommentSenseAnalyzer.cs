using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
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
            var documentationCache = new ConcurrentDictionary<ISymbol, XElement>(SymbolEqualityComparer.Default);

            compilationContext.RegisterSymbolAction(c => AnalyzeSymbol(c, documentationCache),
                SymbolKind.NamedType,
                SymbolKind.Method,
                SymbolKind.Property,
                SymbolKind.Field,
                SymbolKind.Event);

            compilationContext.RegisterSyntaxNodeAction(CrefAnalyzer.Analyze, SyntaxKind.XmlCrefAttribute);
            compilationContext.RegisterSyntaxNodeAction(c => DocumentationTextAnalyzer.Analyze(c, analyzedNodes), SyntaxKind.XmlText);
        });
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context, ConcurrentDictionary<ISymbol, XElement> documentationCache)
    {
        var symbol = context.Symbol;
        SyntaxTree? tree = (from location in symbol.Locations where !location.SourceTree.IsDocumentationModeNone() select location.SourceTree).FirstOrDefault();

        if (tree is null)
            return;

        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);
        var locationCache = new DocumentationLocationCache();
        AnalyzeSymbolCore(context, symbol, options, documentationCache, locationCache);
    }

    private static void AnalyzeSymbolCore(SymbolAnalysisContext context, ISymbol symbol, CommentSenseOptions options, ConcurrentDictionary<ISymbol, XElement> documentationCache, DocumentationLocationCache locationCache)
    {
        if (!IsEligibleForAnalysis(symbol, options))
            return;

        if (!documentationCache.TryGetValue(symbol, out var element))
        {
            var xml = symbol.GetDocumentationCommentXml();
            if (!DocumentationXmlExtensions.TryParseDocumentation(xml, out element))
            {
                // Parsing failure (e.g., malformed XML) is treated as missing documentation
                ReportMissingDocumentation(context, symbol, options);
                return;
            }

            documentationCache.TryAdd(symbol, element);
        }

        TagOrderAnalyzer.Analyze(context, symbol, element, options, locationCache);

        if (!DocumentationXmlExtensions.HasValidDocumentation(element))
        {
            // Documentation is present but does not contain valid tags (e.g., empty or only unsupported tags)
            ReportMissingDocumentation(context, symbol, options);
            return;
        }

        if (DocumentationXmlExtensions.HasInheritDoc(element) &&
            !DocumentationXmlExtensions.HasInheritDocWithCref(element) &&
            !symbol.IsInheriting())
        {
            ReportMissingDocs(context, symbol);
            return;
        }

        SummaryAnalyzer.Analyze(context, symbol, element, options, locationCache);
        AnalyzeSpecificSymbol(context, symbol, element, options, locationCache);
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

    private static void AnalyzeSpecificSymbol(SymbolAnalysisContext context, ISymbol symbol, System.Xml.Linq.XElement element, CommentSenseOptions options, DocumentationLocationCache locationCache)
    {
        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                ParameterAnalyzer.Analyze(context, methodSymbol.Parameters, methodSymbol, element, options, locationCache);
                TypeParameterAnalyzer.Analyze(context, methodSymbol.TypeParameters, methodSymbol, element, options, locationCache);
                ReturnValueAnalyzer.Analyze(context, methodSymbol, element, options, locationCache);
                ExceptionAnalyzer.Analyze(context, methodSymbol, element, options, locationCache, isPrimaryCtor: methodSymbol.IsPrimaryConstructor());
                break;
            case IPropertySymbol propertySymbol:
                if (propertySymbol.IsIndexer)
                {
                    ParameterAnalyzer.Analyze(context, propertySymbol.Parameters, propertySymbol, element, options, locationCache);
                }
                ReturnValueAnalyzer.Analyze(context, propertySymbol, element, options, locationCache);
                ExceptionAnalyzer.Analyze(context, propertySymbol, element, options, locationCache);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                TypeParameterAnalyzer.Analyze(context, namedTypeSymbol.TypeParameters, namedTypeSymbol, element, options, locationCache);
                if (namedTypeSymbol is { TypeKind: TypeKind.Delegate, DelegateInvokeMethod: not null })
                {
                    ParameterAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod.Parameters, namedTypeSymbol, element, options, locationCache);
                    ReturnValueAnalyzer.Analyze(context, namedTypeSymbol.DelegateInvokeMethod, namedTypeSymbol, element, options, locationCache);
                }

                if (namedTypeSymbol.GetPrimaryConstructor() is { } primaryCtor)
                {
                    ParameterAnalyzer.Analyze(context, primaryCtor.Parameters, namedTypeSymbol, element, options, locationCache);
                    ReturnValueAnalyzer.Analyze(context, primaryCtor, namedTypeSymbol, element, options, locationCache);
                    ExceptionAnalyzer.Analyze(context, namedTypeSymbol, element, options, locationCache, isPrimaryCtor: true);
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
