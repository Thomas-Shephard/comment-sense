using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class SymbolExtensions
{
    public static ImmutableArray<IParameterSymbol> GetParameters(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.Parameters,
            IPropertySymbol p => p.Parameters,
            INamedTypeSymbol { DelegateInvokeMethod: { } m } => m.Parameters,
            INamedTypeSymbol t when t.GetPrimaryConstructor() is { } c => c.Parameters,
            _ => []
        };
    }

    public static ImmutableArray<ITypeParameterSymbol> GetTypeParameters(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol m => m.TypeParameters,
            INamedTypeSymbol t => t.TypeParameters,
            _ => []
        };
    }

    public static IMethodSymbol? GetPrimaryConstructor(this INamedTypeSymbol type)
    {
        if (type.TypeKind is TypeKind.Class or TypeKind.Struct)
        {
            return type.InstanceConstructors.FirstOrDefault(constructor => constructor.IsPrimaryConstructor());
        }

        return null;
    }

    public static bool IsPrimaryConstructor(this ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => method.IsPrimaryConstructor(),
            INamedTypeSymbol type => type.GetPrimaryConstructor() != null,
            _ => false
        };
    }

    public static bool IsPrimaryConstructor(this IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Constructor)
        {
            return false;
        }

        return method.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax);
    }

    public static ISymbol? GetAssociatedSymbol(this SyntaxNode node, SemanticModel semanticModel)
    {
        if (node is VariableDeclaratorSyntax declarator)
        {
            return semanticModel.GetDeclaredSymbol(declarator);
        }

        var memberDecl = node.GetMemberDeclaration();
        if (memberDecl is null)
        {
            return null;
        }

        if (memberDecl is BaseFieldDeclarationSyntax fieldDecl)
        {
            var variables = fieldDecl.Declaration.Variables;
            if (variables.Count == 0)
            {
                return null;
            }

            var variable = variables.FirstOrDefault(v => v.Span.Contains(node.Span));
            if (variable != null)
            {
                return semanticModel.GetDeclaredSymbol(variable);
            }

            return semanticModel.GetDeclaredSymbol(variables[0]);
        }

        return semanticModel.GetDeclaredSymbol(memberDecl);
    }

    public static bool InheritsFromOrEquals(this ITypeSymbol type, ITypeSymbol baseType)
    {
        if (AreEquivalent(type, baseType))
            return true;

        if (baseType.TypeKind == TypeKind.Interface)
        {
            return type.AllInterfaces.Any(i => AreEquivalent(i, baseType));
        }

        var current = type.BaseType;
        while (current != null)
        {
            if (AreEquivalent(current, baseType))
                return true;

            current = current.BaseType;
        }

        return false;

        static bool AreEquivalent(ITypeSymbol t, ITypeSymbol b)
        {
            if (SymbolEqualityComparer.Default.Equals(t, b))
                return true;

            return (b.IsDefinition && SymbolEqualityComparer.Default.Equals(t.OriginalDefinition, b)) ||
                   (t.IsDefinition && SymbolEqualityComparer.Default.Equals(b.OriginalDefinition, t));
        }
    }

    public static ImmutableArray<string> GetExpectedMemberNames(this ISymbol symbol, string tagName)
    {
        return tagName switch
        {
            DocumentationTags.Param => [.. symbol.GetParameters().Select(p => p.Name)],
            DocumentationTags.TypeParam => [.. symbol.GetTypeParameters().Select(p => p.Name)],
            _ => []
        };
    }
}
