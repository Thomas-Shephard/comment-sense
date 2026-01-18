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
            switch (current.DeclaredAccessibility)
            {
                case Accessibility.NotApplicable:
                case Accessibility.Public:
                case Accessibility.Protected:
                case Accessibility.ProtectedOrInternal:
                    break;
                case Accessibility.Private:
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Internal:
                default:
                    return false;
            }
            current = current.ContainingSymbol;
        }
        return true;
    }
}
