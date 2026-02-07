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
}
