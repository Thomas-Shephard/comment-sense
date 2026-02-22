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

        var crefAttr = GetCrefAttribute(node);
        if (crefAttr != null)
        {
            AnalyzeCref(context, associatedSymbol, crefAttr);
        }
        else
        {
            AnalyzeImplicit(context, associatedSymbol, node.GetLocation());
        }
    }

    private static XmlCrefAttributeSyntax? GetCrefAttribute(XmlNodeSyntax node)
    {
        return node switch
        {
            XmlElementSyntax e => e.StartTag.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault(),
            XmlEmptyElementSyntax ee => ee.Attributes.OfType<XmlCrefAttributeSyntax>().FirstOrDefault(),
            _ => null
        };
    }

    private static void AnalyzeCref(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, XmlCrefAttributeSyntax crefAttr)
    {
        var symbolInfo = context.SemanticModel.GetSymbolInfo(crefAttr.Cref, context.CancellationToken);
        var target = symbolInfo.Symbol ?? (symbolInfo.CandidateSymbols.Length == 1 ? symbolInfo.CandidateSymbols[0] : null);

        if (target != null && HasDocumentation(target))
            return;

        var location = crefAttr.Parent?.GetLocation() ?? crefAttr.GetLocation();
        ReportInvalidTarget(context, associatedSymbol, location);
    }

    private static void AnalyzeImplicit(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, Location location)
    {
        if (!associatedSymbol.GetInheritedSymbols().Any(HasDocumentation))
            ReportInvalidTarget(context, associatedSymbol, location);
    }

    private static bool HasDocumentation(ISymbol symbol)
    {
        return DocumentationXmlExtensions.HasValidDocumentation(symbol.GetDocumentationCommentXml());
    }

    private static void ReportInvalidTarget(SyntaxNodeAnalysisContext context, ISymbol associatedSymbol, Location location)
    {
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidInheritDocTargetRule, location, associatedSymbol.GetDisplayName()));
    }
}
