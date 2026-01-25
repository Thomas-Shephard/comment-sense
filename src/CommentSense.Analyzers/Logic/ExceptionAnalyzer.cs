using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ExceptionAnalyzer
{
    private const string ExceptionTag = "exception";

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, ImmutableHashSet<string> ignoredExceptions, ImmutableHashSet<string> customLowQualityTerms, bool isPrimaryCtor = false)
    {
        var documentedExceptionElements = DocumentationExtensions.GetTargetElements(xml, ExceptionTag).ToList();
        var documentedTypes = GetDocumentedExceptionTypes(context, documentedExceptionElements);
        var thrownTypes = GetThrownTypes(context, symbol, isPrimaryCtor);

        // CSENSE012: Missing Exception Documentation
        foreach (var thrownType in thrownTypes.Where(t => !documentedTypes.Any(t.InheritsFromOrEquals) &&
                                                         !ignoredExceptions.Contains(t.Name) &&
                                                         !ignoredExceptions.Contains(t.ToDisplayString())))
        {
            var location = symbol.Locations.GetPrimaryLocation();
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingExceptionDocumentationRule, location, thrownType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
        }

        // CSENSE016: Low Quality Exception Documentation
        foreach (var exceptionElement in documentedExceptionElements)
        {
            var cref = exceptionElement.Attribute("cref")?.Value;
            if (cref is null || string.IsNullOrWhiteSpace(cref))
                continue;

            var resolved = ResolveExceptionType(cref, context.Compilation);
            if (resolved != null && QualityAnalyzer.IsLowQuality(exceptionElement, resolved.Name, customLowQualityTerms))
            {
                QualityAnalyzer.Report(context, symbol, ExceptionTag, resolved.Name);
            }
        }
    }

    private static HashSet<ITypeSymbol> GetDocumentedExceptionTypes(SymbolAnalysisContext context, IEnumerable<XElement> exceptionElements)
    {
        return new HashSet<ITypeSymbol>(
            exceptionElements
                .Select(e => e.Attribute("cref")?.Value)
                .Where(cref => !string.IsNullOrWhiteSpace(cref))
                .OfType<string>()
                .Select(cref => ResolveExceptionType(cref, context.Compilation))
                .OfType<ITypeSymbol>(),
            SymbolEqualityComparer.Default);
    }

    private static ITypeSymbol? ResolveExceptionType(string cref, Compilation compilation)
    {
        var resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, compilation);
        if (resolved is ITypeSymbol ts)
        {
            return ts;
        }

        return ResolveExceptionTypeFallback(cref, compilation);
    }

    private static ITypeSymbol? ResolveExceptionTypeFallback(string cref, Compilation compilation)
    {
        // Strip the ID prefix (e.g., "T:", "M:", "!:") if present
        var typeName = cref;
        if (cref.Length > 2 && cref[1] == ':')
            typeName = cref.Substring(2);

        // Extract simple name to use fast lookup
        var parts = typeName.Split('.');
        var simpleName = parts[parts.Length - 1];

        // Try direct lookup
        var type = compilation.GetTypeByMetadataName(typeName);
        if (type != null)
            return type;

        // Try lookup by name (e.g. "ArgumentNullException" instead of "System.ArgumentNullException")
        return compilation.GetSymbolsWithName(simpleName, SymbolFilter.Type)
                   .OfType<ITypeSymbol>()
                   .FirstOrDefault(t => (t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) == typeName || t.Name == typeName) && !t.IsImplicitlyDeclared);
    }

    private static HashSet<ITypeSymbol> GetThrownTypes(SymbolAnalysisContext context, ISymbol symbol, bool isPrimaryCtor)
    {
        var thrownTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = syntaxReference.GetSyntax();
            var semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);

            var nodes = GetDescendantNodesOfInterest(syntax, isPrimaryCtor);
            var exceptions = IdentifyThrownExceptions(nodes, semanticModel, context.CancellationToken);

            thrownTypes.UnionWith(exceptions);
        }

        return thrownTypes;
    }

    private static IEnumerable<SyntaxNode> GetDescendantNodesOfInterest(SyntaxNode root, bool isPrimaryCtor)
    {
        return root.DescendantNodes(n =>
        {
            // Ensure we don't block the root node (ClassDeclaration is BaseTypeDeclaration)
            if (n == root)
                return true;

            if (n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseTypeDeclarationSyntax)
                return false;

            if (isPrimaryCtor && IsExcludedPrimaryConstructorMember(n))
                return false;

            return true;
        });
    }

    private static bool IsExcludedPrimaryConstructorMember(SyntaxNode n)
    {
        // Block members that have their own analysis to avoid duplicates.
        // We descend into FieldDeclaration because fields don't have their own ExceptionAnalyzer.
        return n is MethodDeclarationSyntax
               or ConstructorDeclarationSyntax
               or PropertyDeclarationSyntax
               or IndexerDeclarationSyntax
               or AccessorListSyntax
               or EventDeclarationSyntax
               or ArrowExpressionClauseSyntax;
    }

    private static IEnumerable<ITypeSymbol> IdentifyThrownExceptions(IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel, CancellationToken token)
    {
        foreach (var node in nodes)
        {
            ITypeSymbol? type;

            switch (node)
            {
                case ThrowStatementSyntax ts:
                    type = ts.Expression is not null
                        ? semanticModel.GetTypeInfo(ts.Expression, token).Type
                        : GetCaughtExceptionType(ts, semanticModel, token);
                    break;
                case ThrowExpressionSyntax te:
                    type = semanticModel.GetTypeInfo(te.Expression, token).Type;
                    break;
                default:
                    continue;
            }

            if (type is not null && !IsCaughtLocally(node, type, semanticModel))
            {
                yield return type;
            }
        }
    }

    private static bool IsCaughtLocally(SyntaxNode throwNode, ITypeSymbol thrownType, SemanticModel semanticModel)
    {
        var current = throwNode.Parent;
        while (current != null)
        {
            switch (current)
            {
                case TryStatementSyntax tryStatement:
                {
                    // Only consider it caught if the throw is inside the 'try' block
                    // (Exceptions thrown in catch/finally blocks escape this try statement)
                    if (tryStatement.Block.Span.Contains(throwNode.Span))
                    {
                        var isCaught = tryStatement.Catches
                            .Where(c => c.Filter == null)
                            .Any(c => c.Declaration == null ||
                                      (semanticModel.GetTypeInfo(c.Declaration.Type).Type is { } caughtType &&
                                       thrownType.InheritsFromOrEquals(caughtType)));

                        if (isCaught)
                            return true;
                    }

                    break;
                }
                case MethodDeclarationSyntax:
                case LocalFunctionStatementSyntax:
                case ConstructorDeclarationSyntax:
                case AccessorDeclarationSyntax:
                    // Stop at method boundary
                    return false;
            }

            current = current.Parent;
        }

        return false;
    }

    private static ITypeSymbol? GetCaughtExceptionType(ThrowStatementSyntax throwStatement, SemanticModel semanticModel, CancellationToken cancellationToken)
    {
        var catchClause = throwStatement.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause?.Declaration is null)
        {
            return semanticModel.Compilation.GetTypeByMetadataName("System.Exception");
        }

        return semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
    }
}
