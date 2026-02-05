using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class DocumentationTextAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var xmlText = (XmlTextSyntax)context.Node;

        var memberDecl = xmlText.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDecl is null)
            return;

        var tree = context.Node.SyntaxTree;
        var options = AnalyzerOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);

        var symbol = memberDecl is BaseFieldDeclarationSyntax { Declaration.Variables.Count: > 0 } fieldDecl
            ? context.SemanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0])
            : context.SemanticModel.GetDeclaredSymbol(memberDecl);

        if (symbol is null || !symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        LangwordAnalyzer.Analyze(context, xmlText, options);
        GhostReferenceAnalyzer.Analyze(context, xmlText, symbol, options);
    }
}
