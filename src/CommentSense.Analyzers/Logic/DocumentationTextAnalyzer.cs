using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CommentSense.Core;
using System.Collections.Concurrent;

namespace CommentSense.Analyzers.Logic;

internal static class DocumentationTextAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context, ConcurrentDictionary<XmlTextSyntax, bool> analyzedNodes)
    {
        var xmlText = (XmlTextSyntax)context.Node;

        // Ensure each XmlText node is only analyzed once per analysis pass.
        // This avoids duplicate diagnostics when the same documentation is associated with multiple symbols.
        if (!analyzedNodes.TryAdd(xmlText, true))
            return;

        var memberDecl = xmlText.GetMemberDeclaration();
        if (memberDecl is null)
            return;

        var tree = context.Node.SyntaxTree;
        var options = CommentSenseOptions.GetOptions(context.Options.AnalyzerConfigOptionsProvider, tree);

        var symbol = memberDecl is BaseFieldDeclarationSyntax { Declaration.Variables.Count: > 0 } fieldDecl
            ? context.SemanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0])
            : context.SemanticModel.GetDeclaredSymbol(memberDecl);

        if (symbol is null || !symbol.IsEligibleForAnalysis(options.VisibilityLevel))
            return;

        LangwordAnalyzer.Analyze(context, xmlText, options);
        GhostReferenceAnalyzer.Analyze(context, xmlText, symbol, options);
    }
}
