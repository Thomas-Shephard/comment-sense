using System.Collections.Immutable;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Analyzers;

internal static class AnalyzerExtensions
{
    public static bool IsEligibleForAnalysis(this ISymbol symbol, bool includeInternal = false)
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

        return symbol.IsEffectivelyAccessible(includeInternal);
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

    public static bool IsInExceptionTag(this XmlCrefAttributeSyntax crefAttribute)
    {
        var localName = crefAttribute.Parent switch
        {
            XmlEmptyElementSyntax emptyElement => emptyElement.Name.LocalName.ValueText,
            XmlElementStartTagSyntax startTag => startTag.Name.LocalName.ValueText,
            _ => null
        };

        return localName == "exception";
    }

    public static bool IsDocumentationModeNone([System.Diagnostics.CodeAnalysis.NotNullWhen(false)] this SyntaxTree? tree)
        => tree?.Options.DocumentationMode is null or DocumentationMode.None;

    public static bool IsInheriting(this ISymbol symbol)
    {
        if (symbol.IsOverride)
            return true;

        if (IsExplicitInterfaceImplementation(symbol))
            return true;

        if (symbol is INamedTypeSymbol typeSymbol)
            return IsInheritingType(typeSymbol);

        var containingType = symbol.ContainingType;
        if (containingType == null || containingType.AllInterfaces.IsEmpty)
            return false;

        if (containingType.TypeKind == TypeKind.Interface)
            return IsInheritingInterfaceMember(symbol, containingType);

        return IsImplicitInterfaceImplementation(symbol, containingType);
    }

    private static bool IsExplicitInterfaceImplementation(ISymbol symbol) => symbol switch
    {
        IMethodSymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
        IPropertySymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
        IEventSymbol { ExplicitInterfaceImplementations.Length: > 0 } => true,
        _ => false
    };

    private static bool IsInheritingType(INamedTypeSymbol typeSymbol)
    {
        return (typeSymbol.BaseType != null &&
               typeSymbol.BaseType.SpecialType != SpecialType.System_Object &&
               typeSymbol.BaseType.SpecialType != SpecialType.System_ValueType &&
               typeSymbol.BaseType.SpecialType != SpecialType.System_Enum &&
               typeSymbol.BaseType.SpecialType != SpecialType.System_MulticastDelegate) ||
               !typeSymbol.Interfaces.IsEmpty;
    }

    private static bool IsInheritingInterfaceMember(ISymbol symbol, INamedTypeSymbol containingType)
    {
        return containingType.AllInterfaces.Any(i => i.GetMembers(symbol.Name).Any(m => MatchesInterfaceMember(symbol, m)));
    }

    private static bool MatchesInterfaceMember(ISymbol symbol, ISymbol baseMember)
    {
        if (baseMember.Kind != symbol.Kind)
            return false;

        return symbol switch
        {
            IEventSymbol eventSymbol when baseMember is IEventSymbol baseEvent             => SymbolEqualityComparer.Default.Equals(baseEvent.Type, eventSymbol.Type),
            IMethodSymbol methodSymbol when baseMember is IMethodSymbol baseMethod         => MatchesMethod(methodSymbol, baseMethod),
            IPropertySymbol propertySymbol when baseMember is IPropertySymbol baseProperty => MatchesProperty(propertySymbol, baseProperty),
            _                                                                              => false
        };
    }

    private static bool MatchesMethod(IMethodSymbol method, IMethodSymbol baseMethod)
    {
        if (baseMethod.IsStatic != method.IsStatic)
            return false;

        if (baseMethod.ReturnsByRef != method.ReturnsByRef || baseMethod.RefKind != method.RefKind)
            return false;

        if (baseMethod.TypeParameters.Length != method.TypeParameters.Length)
            return false;

        if (baseMethod.Parameters.Length != method.Parameters.Length)
            return false;

        var substitutedBase = baseMethod;
        if (method.TypeParameters.Length > 0)
            substitutedBase = baseMethod.Construct([.. method.TypeParameters]);

        if (!SymbolEqualityComparer.Default.Equals(substitutedBase.ReturnType, method.ReturnType) && !method.ReturnType.InheritsFromOrEquals(substitutedBase.ReturnType))
            return false;

        return !substitutedBase.Parameters.Where((t, j) => t.RefKind != method.Parameters[j].RefKind || !SymbolEqualityComparer.Default.Equals(t.Type, method.Parameters[j].Type)).Any();
    }

    private static bool MatchesProperty(IPropertySymbol property, IPropertySymbol baseProperty)
    {
        if (baseProperty.IsStatic != property.IsStatic)
            return false;

        if (baseProperty.ReturnsByRef != property.ReturnsByRef || baseProperty.RefKind != property.RefKind)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(baseProperty.Type, property.Type) && !property.Type.InheritsFromOrEquals(baseProperty.Type))
            return false;

        if (baseProperty.Parameters.Length != property.Parameters.Length)
            return false;

        return !baseProperty.Parameters.Where((t, j) => !SymbolEqualityComparer.Default.Equals(t.Type, property.Parameters[j].Type)).Any();
    }

    private static bool IsImplicitInterfaceImplementation(ISymbol symbol, INamedTypeSymbol containingType)
    {
        if (symbol.DeclaredAccessibility != Accessibility.Public)
            return false;

        return containingType.AllInterfaces
            .SelectMany(i => i.GetMembers(symbol.Name))
            .Where(m => m.Kind == symbol.Kind)
            .Any(m => SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(m), symbol));
    }
}
