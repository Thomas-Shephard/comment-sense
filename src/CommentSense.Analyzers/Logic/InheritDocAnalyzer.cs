using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class InheritDocAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var node = (XmlNodeSyntax)context.Node;
        if (node.GetTagName() != DocumentationTags.InheritDoc)
            return;

        var associatedSymbol = node.GetAssociatedSymbol(context.SemanticModel);
        if (associatedSymbol is null)
            return;

        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, node.SyntaxTree);
        if (!associatedSymbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        var crefAttr = node switch
        {
            XmlElementSyntax e => e.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault(),
            XmlEmptyElementSyntax ee => ee.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault(),
            _ => null
        };

        if (crefAttr != null)
        {
            var symbolInfo = context.SemanticModel.GetSymbolInfo(crefAttr.Cref, context.CancellationToken);
            var target = symbolInfo.Symbol ?? (symbolInfo.CandidateSymbols.Length == 1 ? symbolInfo.CandidateSymbols[0] : null);

            if (target == null)
            {
                ReportInvalidTarget(context, associatedSymbol, node.GetLocation());
                return;
            }

            if (!HasDocumentation(target))
                ReportInvalidTarget(context, associatedSymbol, node.GetLocation());
        }
        else
        {
            if (!associatedSymbol.GetInheritedSymbols().Any(HasDocumentation))
                ReportInvalidTarget(context, associatedSymbol, node.GetLocation());
        }
    }

    private static bool HasDocumentation(ISymbol symbol)
    {
        var xml = symbol.GetDocumentationCommentXml();
        if (string.IsNullOrWhiteSpace(xml))
            return false;

        if (!DocumentationXmlExtensions.TryParseDocumentation(xml, out var element))
            return false;

        return DocumentationXmlExtensions.HasValidDocumentation(element);
    }

    private static void ReportInvalidTarget(SyntaxNodeAnalysisContext context, ISymbol symbol, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidInheritDocTargetRule, location, symbol.GetDisplayName()));
    }
}
