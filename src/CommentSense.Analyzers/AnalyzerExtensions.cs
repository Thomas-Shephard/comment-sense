using System.Collections.Immutable;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers;

internal static class AnalyzerExtensions
{
    public static bool IsEligibleForAnalysis(this ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
            return false;

        if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise })
            return false;

        return symbol.IsEffectivelyAccessible();
    }

    public static Location GetPrimaryLocation(this ImmutableArray<Location> locations)
    {
        if (locations.Length == 0)
            return Location.None;

        return locations[0];
    }
}
