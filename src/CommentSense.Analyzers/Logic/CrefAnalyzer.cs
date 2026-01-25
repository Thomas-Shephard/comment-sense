using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class CrefAnalyzer
{
    public static void Analyze(SyntaxNodeAnalysisContext context)
    {
        var crefAttribute = (XmlCrefAttributeSyntax)context.Node;

        if (!IsEligible(crefAttribute, context))
            return;

        var cref = crefAttribute.Cref;
        var symbolInfo = context.SemanticModel.GetSymbolInfo(cref, context.CancellationToken);
        var symbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        if (symbol is null)
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.UnresolvedCrefRule, cref.GetLocation(), cref.ToString()));
            return;
        }

        if (IsInsideExceptionTag(crefAttribute) && !IsException(symbol, context.Compilation))
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidExceptionCrefRule, cref.GetLocation(), cref.ToString()));
        }
    }

    private static bool IsEligible(XmlCrefAttributeSyntax crefAttribute, SyntaxNodeAnalysisContext context)
    {
        ISymbol? symbol;
        var memberDecl = crefAttribute.FirstAncestorOrSelf<MemberDeclarationSyntax>();
        if (memberDecl is not null)
        {
            symbol = memberDecl is BaseFieldDeclarationSyntax { Declaration.Variables.Count: > 0 } fieldDecl
                ? context.SemanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0])
                : context.SemanticModel.GetDeclaredSymbol(memberDecl);

            return symbol is not null && symbol.IsEligibleForAnalysis();
        }

        var namespaceDecl = crefAttribute.FirstAncestorOrSelf<BaseNamespaceDeclarationSyntax>();
        if (namespaceDecl is null)
            return false;

        symbol = context.SemanticModel.GetDeclaredSymbol(namespaceDecl);
        return symbol is not null && symbol.IsEligibleForAnalysis();
    }

    private static bool IsInsideExceptionTag(XmlCrefAttributeSyntax crefAttribute)
    {
        var parent = crefAttribute.Parent;
        var name = parent switch
        {
            XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText,
            XmlElementStartTagSyntax startTag => startTag.Name.LocalName.ValueText,
            _ => null
        };

        return name == "exception";
    }

    private static bool IsException(ISymbol symbol, Compilation compilation)
    {
        var type = symbol switch
        {
            ITypeSymbol t => t,
            IAliasSymbol a => a.Target as ITypeSymbol,
            _ => null
        };

        if (type is null)
            return false;

        if (type.TypeKind is TypeKind.Error or TypeKind.TypeParameter)
            return true;

        var exceptionType = compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
            return true;

        for (var current = type; current is not null; current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, exceptionType))
                return true;
        }

        return false;
    }
}
