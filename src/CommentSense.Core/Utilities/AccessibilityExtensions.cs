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
        while (true)
        {
            if (symbol is null)
                return VisibilityLevel.Private;

            if (symbol is IArrayTypeSymbol array)
            {
                symbol = array.ElementType;
                continue;
            }

            if (symbol is IPointerTypeSymbol pointer)
            {
                symbol = pointer.PointedAtType;
                continue;
            }

            if (symbol.Kind is SymbolKind.Local or SymbolKind.Label or SymbolKind.RangeVariable)
                return VisibilityLevel.Private;

            var mostRestrictive = VisibilityLevel.Public;

            if (symbol is INamedTypeSymbol { TypeArguments.IsEmpty: false } namedType)
            {
                foreach (var typeArg in namedType.TypeArguments)
                {
                    if (typeArg.Kind == SymbolKind.TypeParameter)
                        continue;

                    var argLevel = typeArg.GetEffectiveVisibilityLevel();
                    if (argLevel > mostRestrictive)
                        mostRestrictive = argLevel;
                }
            }

            var current = symbol;
            while (current is not null && current.Kind is not SymbolKind.Namespace)
            {
                if (current.DeclaredAccessibility != Accessibility.NotApplicable)
                {
                    var level = current.DeclaredAccessibility switch
                    {
                        Accessibility.Public => VisibilityLevel.Public,
                        Accessibility.Protected or Accessibility.ProtectedOrInternal => VisibilityLevel.Protected,
                        Accessibility.Internal or Accessibility.ProtectedAndInternal => VisibilityLevel.Internal,
                        Accessibility.Private => VisibilityLevel.Private,
                        _ => VisibilityLevel.Public
                    };

                    if (level > mostRestrictive)
                        mostRestrictive = level;

                    if (mostRestrictive == VisibilityLevel.Private)
                        break;
                }

                current = current.ContainingSymbol;
            }

            return mostRestrictive;
        }
    }
}
