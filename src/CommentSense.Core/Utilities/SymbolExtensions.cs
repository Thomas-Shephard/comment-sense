using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
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
        if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            return null;

        return type.InstanceConstructors.FirstOrDefault(constructor => constructor.IsPrimaryConstructor());
    }

    public static bool IsPrimaryConstructor(this IMethodSymbol method)
    {
        if (method.MethodKind != MethodKind.Constructor)
            return false;

        return method.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is TypeDeclarationSyntax);
    }

    private static ISymbol? GetDeclaredMemberSymbol(this SemanticModel semanticModel, MemberDeclarationSyntax memberDecl, SyntaxNode? originalNode = null)
    {
        if (memberDecl is not BaseFieldDeclarationSyntax fieldDecl)
            return semanticModel.GetDeclaredSymbol(memberDecl);

        if (fieldDecl.Declaration.Variables.Count == 0)
            return null;

        if (originalNode != null)
        {
            var variable = fieldDecl.Declaration.Variables.FirstOrDefault(v => v.Span.Contains(originalNode.Span));
            if (variable != null)
                return semanticModel.GetDeclaredSymbol(variable);
        }

        return semanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables[0]);
    }

    public static ISymbol? GetAssociatedSymbol(this SyntaxNode node, SemanticModel semanticModel)
    {
        var variableDeclarator = node.FirstAncestorOrSelf<VariableDeclaratorSyntax>();
        if (variableDeclarator != null)
            return semanticModel.GetDeclaredSymbol(variableDeclarator);

        var memberDecl = node.GetMemberDeclaration();
        return memberDecl is null ? null : semanticModel.GetDeclaredMemberSymbol(memberDecl, node);
    }

    public static bool InheritsFromOrEquals(this ITypeSymbol type, ITypeSymbol baseType)
    {
        if (SymbolEqualityComparer.Default.Equals(type, baseType))
            return true;

        if (baseType.TypeKind == TypeKind.Interface)
            return type.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, baseType));

        var current = type.BaseType;
        while (current != null)
        {
            if (SymbolEqualityComparer.Default.Equals(current, baseType))
                return true;

            current = current.BaseType;
        }
        return false;
    }
}
