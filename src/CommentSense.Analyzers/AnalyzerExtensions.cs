using System.Collections.Immutable;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Analyzers;

internal static class AnalyzerExtensions
{
    public static bool IsEligibleForAnalysis(this ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
            return false;

        if (symbol.ContainingNamespace?.ToDisplayString() == "System.Runtime.CompilerServices")
            return false;

        switch (symbol)
        {
            case IMethodSymbol method:
                if (method.MethodKind is not (MethodKind.Ordinary or MethodKind.Constructor or MethodKind.UserDefinedOperator or MethodKind.Conversion or MethodKind.DelegateInvoke))
                    return false;

                if (method.IsPrimaryConstructor())
                    return false;

                break;
            case IPropertySymbol or IFieldSymbol:
            {
                if (symbol.ContainingType is { IsRecord: true } && symbol.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is ParameterSyntax))
                    return false;

                break;
            }
        }

        return symbol.IsEffectivelyAccessible();
    }

    public static Location GetPrimaryLocation(this ImmutableArray<Location> locations)
    {
        if (locations.Length == 0)
            return Location.None;

        return locations[0];
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
