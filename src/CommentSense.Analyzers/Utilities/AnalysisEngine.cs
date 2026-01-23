using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers.Utilities;

internal static class AnalysisEngine
{
    public static bool IsEligibleForAnalysis(ISymbol symbol)
    {
        if (symbol.IsImplicitlyDeclared)
            return false;

        if (symbol is IMethodSymbol { MethodKind: MethodKind.PropertyGet or MethodKind.PropertySet or MethodKind.EventAdd or MethodKind.EventRemove or MethodKind.EventRaise })
            return false;

        return symbol.IsEffectivelyAccessible();
    }

    public static Location GetPrimaryLocation(ImmutableArray<Location> locations)
    {
        if (locations.Length == 0)
            return Location.None;

        return locations[0];
    }
}
