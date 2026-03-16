using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers;

internal static class InterfaceMemberMatchingExtensions
{
    public static bool MatchesInterfaceMemberSignature(this ISymbol symbol, ISymbol interfaceMember)
    {
        if (interfaceMember.Kind != symbol.Kind)
            return false;

        return symbol switch
        {
            IEventSymbol eventSymbol when interfaceMember is IEventSymbol baseEvent =>
                SymbolEqualityComparer.Default.Equals(baseEvent.Type, eventSymbol.Type),
            IMethodSymbol methodSymbol when interfaceMember is IMethodSymbol baseMethod =>
                MatchesMethodSignature(methodSymbol, baseMethod),
            IPropertySymbol propertySymbol when interfaceMember is IPropertySymbol baseProperty =>
                MatchesPropertySignature(propertySymbol, baseProperty),
            _ => false
        };
    }

    private static bool MatchesMethodSignature(IMethodSymbol method, IMethodSymbol baseMethod)
    {
        if (baseMethod.IsStatic != method.IsStatic)
            return false;

        if (baseMethod.ReturnsByRef != method.ReturnsByRef || baseMethod.RefKind != method.RefKind)
            return false;

        if (baseMethod.TypeParameters.Length != method.TypeParameters.Length)
            return false;

        if (baseMethod.Parameters.Length != method.Parameters.Length)
            return false;

        var substitutedBaseMethod = baseMethod;
        if (method.TypeParameters.Length > 0)
            substitutedBaseMethod = baseMethod.Construct([.. method.TypeParameters]);

        if (!SymbolEqualityComparer.Default.Equals(substitutedBaseMethod.ReturnType, method.ReturnType) &&
            !method.ReturnType.InheritsFromOrEquals(substitutedBaseMethod.ReturnType))
        {
            return false;
        }

        for (int i = 0; i < substitutedBaseMethod.Parameters.Length; i++)
        {
            var baseParameter = substitutedBaseMethod.Parameters[i];
            var parameter = method.Parameters[i];

            if (baseParameter.RefKind != parameter.RefKind)
                return false;

            if (!SymbolEqualityComparer.Default.Equals(baseParameter.Type, parameter.Type))
                return false;
        }

        return true;
    }

    private static bool MatchesPropertySignature(IPropertySymbol property, IPropertySymbol baseProperty)
    {
        if (baseProperty.IsStatic != property.IsStatic)
            return false;

        if (baseProperty.ReturnsByRef != property.ReturnsByRef || baseProperty.RefKind != property.RefKind)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(baseProperty.Type, property.Type) &&
            !property.Type.InheritsFromOrEquals(baseProperty.Type))
        {
            return false;
        }

        if (baseProperty.Parameters.Length != property.Parameters.Length)
            return false;

        for (int i = 0; i < baseProperty.Parameters.Length; i++)
        {
            if (!SymbolEqualityComparer.Default.Equals(baseProperty.Parameters[i].Type, property.Parameters[i].Type))
                return false;
        }

        return true;
    }
}
