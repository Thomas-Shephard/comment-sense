using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers;

/// <summary>
/// Suppresses built-in compiler diagnostics that overlap with CommentSense rules.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommentSenseSuppressor : DiagnosticSuppressor
{
    /// <inheritdoc />
    public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions => CommentSenseSuppressions.SupportedSuppressions;

    /// <inheritdoc />
    public override void ReportSuppressions(SuppressionAnalysisContext context)
    {
        foreach (var diagnostic in context.ReportedDiagnostics)
        {
            if (!CommentSenseSuppressions.SuppressionMap.TryGetValue(diagnostic.Id, out var descriptor))
                continue;

            if (ShouldSuppress(diagnostic, context))
                context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
        }
    }

    private static bool ShouldSuppress(Diagnostic diagnostic, SuppressionAnalysisContext context)
    {
        var tree = diagnostic.Location.SourceTree;
        if (tree == null)
            return true;

        var options = AnalyzerOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);
        if (!options.EnableConditionalSuppression)
            return true;

        return IsSymbolEligibleForSuppression(diagnostic, tree, context, options);
    }

    private static bool IsSymbolEligibleForSuppression(Diagnostic diagnostic, SyntaxTree tree, SuppressionAnalysisContext context, CommentSenseOptions options)
    {
        var model = context.GetSemanticModel(tree);
        var root = model.SyntaxTree.GetRoot(context.CancellationToken);
        var node = root.FindNode(diagnostic.Location.SourceSpan);

        var symbol = model.GetDeclaredSymbol(node, context.CancellationToken);
        if (symbol == null)
            return true;

        if (!symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return false;

        if (options.ExcludeConstants && symbol is IFieldSymbol { IsConst: true })
            return false;

        return true;
    }
}
