using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class AccessibilityExtensions
{
    public static bool IsEffectivelyAccessible(this ISymbol? symbol, VisibilityLevel visibilityLevel = VisibilityLevel.Protected)
    {
        if (visibilityLevel is < VisibilityLevel.Public or > VisibilityLevel.Private)
            return false;

        return symbol.GetEffectiveVisibilityLevel() <= visibilityLevel;
    }

    public static VisibilityLevel GetEffectiveVisibilityLevel(this ISymbol? symbol)
    {
        while (symbol is IArrayTypeSymbol or IPointerTypeSymbol)
        {
            if (symbol is IArrayTypeSymbol array)
                symbol = array.ElementType;
            else if (symbol is IPointerTypeSymbol pointer)
                symbol = pointer.PointedAtType;
        }

        if (symbol is null || symbol.Kind is SymbolKind.Local or SymbolKind.Label or SymbolKind.RangeVariable)
            return VisibilityLevel.Private;

        var mostRestrictive = GetTypeArgumentVisibility(symbol);
        if (mostRestrictive == VisibilityLevel.Private)
            return VisibilityLevel.Private;

        return GetHierarchyVisibility(symbol, mostRestrictive);
    }

    private static VisibilityLevel GetTypeArgumentVisibility(ISymbol symbol)
    {
        var typeArguments = symbol switch
        {
            INamedTypeSymbol namedType => namedType.TypeArguments,
            IMethodSymbol method => method.TypeArguments,
            _ => default
        };

        if (typeArguments.IsDefaultOrEmpty)
            return VisibilityLevel.Public;

        var mostRestrictive = VisibilityLevel.Public;
        foreach (var typeArg in typeArguments)
        {
            if (typeArg.Kind == SymbolKind.TypeParameter)
                continue;

            var argLevel = typeArg.GetEffectiveVisibilityLevel();
            if (argLevel > mostRestrictive)
                mostRestrictive = argLevel;
        }

        return mostRestrictive;
    }

    private static VisibilityLevel GetHierarchyVisibility(ISymbol symbol, VisibilityLevel initial)
    {
        var mostRestrictive = initial;
        var current = symbol;
        while (current is not null && current.Kind is not SymbolKind.Namespace)
        {
            if (current.DeclaredAccessibility != Accessibility.NotApplicable)
            {
                var level = MapAccessibilityToVisibilityLevel(current.DeclaredAccessibility);
                if (level > mostRestrictive)
                    mostRestrictive = level;

                if (mostRestrictive == VisibilityLevel.Private)
                    break;
            }

            current = current.ContainingSymbol;
        }

        return mostRestrictive;
    }

    private static VisibilityLevel MapAccessibilityToVisibilityLevel(Accessibility accessibility)
    {
        return accessibility switch
        {
            Accessibility.Public => VisibilityLevel.Public,
            Accessibility.Protected or Accessibility.ProtectedOrInternal => VisibilityLevel.Protected,
            Accessibility.Internal or Accessibility.ProtectedAndInternal => VisibilityLevel.Internal,
            _ => VisibilityLevel.Private
        };
    }
}
