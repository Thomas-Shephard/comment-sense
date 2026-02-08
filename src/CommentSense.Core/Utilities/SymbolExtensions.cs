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
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
        {
            return true;
        }

        if (baseType.TypeKind == TypeKind.Interface)
        {
            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, baseType));
        }

        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
