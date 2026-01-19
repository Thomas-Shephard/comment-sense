using System.Collections.Immutable;
using CommentSense.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentSenseAnalyzer : DiagnosticAnalyzer
{
    private const string DiagnosticId = "CSENSE001";

    private static readonly LocalizableString Title = "Public API is missing documentation";
    private static readonly LocalizableString MessageFormat = "The symbol '{0}' is missing valid documentation";
    private static readonly LocalizableString Description = "Publicly accessible APIs should be documented to ensure maintainability and clarity.";
    private const string Category = "Documentation";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        Title,
        MessageFormat,
        Category,
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: Description);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [Rule];

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
    }

    private static void AnalyzeSymbol(SymbolAnalysisContext context)
    {
        AnalyzeSymbol(context.Symbol, context.ReportDiagnostic);
    }

    private static void AnalyzeSymbol(ISymbol symbol, Action<Diagnostic> reportDiagnostic)
    {
        if (!AnalysisEngine.ShouldReportDiagnostic(symbol))
            return;

        var location = AnalysisEngine.GetPrimaryLocation(symbol.Locations);
        var diagnostic = Diagnostic.Create(Rule, location, symbol.Name);
        reportDiagnostic(diagnostic);
    }
}
