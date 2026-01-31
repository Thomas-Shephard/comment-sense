using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class AccessibilityExtensions
{
    public static bool IsEffectivelyAccessible(this ISymbol? symbol, VisibilityLevel visibilityLevel = VisibilityLevel.Protected)
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
            bool isAccessible = current.DeclaredAccessibility == Accessibility.NotApplicable || visibilityLevel switch
            {
                VisibilityLevel.Public => current.DeclaredAccessibility == Accessibility.Public,
                VisibilityLevel.Protected => current.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal,
                VisibilityLevel.Internal => current.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal or Accessibility.Internal or Accessibility.ProtectedAndInternal,
                VisibilityLevel.Private => true,
                _ => false
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
