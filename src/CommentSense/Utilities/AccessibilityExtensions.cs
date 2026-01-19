using Microsoft.CodeAnalysis;

namespace CommentSense.Utilities;

internal static class AccessibilityExtensions
{
    public static bool IsEffectivelyAccessible(this ISymbol? symbol)
    {
        if (symbol is null)
        {
            return false;
        }

        if (symbol.Kind is SymbolKind.Local or SymbolKind.Label or SymbolKind.RangeVariable)
        {
            return false;
        }

        var current = symbol;
        while (current is not null && current.Kind is not SymbolKind.Namespace)
        {
            var isAccessible = current.DeclaredAccessibility switch
            {
                Accessibility.Private => false,
                Accessibility.ProtectedAndInternal => false,
                Accessibility.Internal => false,
                _ => true
            };

            if (!isAccessible)
            {
                return false;
            }

            current = current.ContainingSymbol;
        }
        return true;
    }
}
