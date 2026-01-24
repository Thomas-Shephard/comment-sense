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

        context.RegisterSymbolAction(AnalyzeSymbol,
            SymbolKind.NamedType,
            SymbolKind.Method,
            SymbolKind.Property,
            SymbolKind.Field,
            SymbolKind.Event);

        context.RegisterSyntaxNodeAction(CrefAnalyzer.Analyze, SyntaxKind.XmlCrefAttribute);
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        var symbol = context.Symbol;

        if (!symbol.IsEligibleForAnalysis())
            return;

        var xml = symbol.GetDocumentationCommentXml();

        if (!DocumentationExtensions.TryParseDocumentation(xml, out var element) || !DocumentationExtensions.HasValidDocumentation(element))
        {
            ReportMissingDocs(context, symbol);
            return;
        }

        if (DocumentationExtensions.HasAutoValidTag(element))
            return;

        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                ParameterAnalyzer.Analyze(context, methodSymbol, element);
                TypeParameterAnalyzer.Analyze(context, methodSymbol.TypeParameters, methodSymbol.Locations, element);
                ReturnValueAnalyzer.Analyze(context, methodSymbol, element);
                break;
            case INamedTypeSymbol namedTypeSymbol:
                TypeParameterAnalyzer.Analyze(context, namedTypeSymbol.TypeParameters, namedTypeSymbol.Locations, element);
                break;
        }
    }

    private static void ReportMissingDocs(SymbolAnalysisContext context, ISymbol symbol)
    {
        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingDocumentationRule, location, symbol.Name));
    }
}
