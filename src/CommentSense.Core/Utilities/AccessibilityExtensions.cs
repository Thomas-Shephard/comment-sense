using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class AccessibilityExtensions
{
    public static bool IsEffectivelyAccessible(this ISymbol? symbol, bool includeInternal = false)
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
            bool isAccessible;
            if (includeInternal)
            {
                // When including internal, only Private is hidden.
                // Internal and ProtectedAndInternal (private protected) are now included.
                isAccessible = current.DeclaredAccessibility != Accessibility.Private;
            }
            else
            {
                isAccessible = current.DeclaredAccessibility switch
                {
                    Accessibility.Private => false,
                    Accessibility.ProtectedAndInternal => false,
                    Accessibility.Internal => false,
                    _ => true
                };
            }

            if (!isAccessible)
            {
                return false;
            }

            current = current.ContainingSymbol;
        }
        return true;
    }
}
