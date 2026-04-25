using System.Diagnostics.CodeAnalysis;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Analyzers.Logic;

internal static class SummaryAnalyzer
{
    private const string GetsPrefix = "Gets";
    private const string SetsPrefix = "Sets";
    private const string GetsOrSetsPrefix = "Gets or sets";
    private const string GetsOrInitializesPrefix = "Gets or initializes";

    private const string BoolValuePrefix = "a value indicating whether";
    private const string BoolGetsPrefix = $"{GetsPrefix} {BoolValuePrefix}";
    private const string BoolSetsPrefix = $"{SetsPrefix} {BoolValuePrefix}";
    private const string BoolGetsOrSetsPrefix = $"{GetsOrSetsPrefix} {BoolValuePrefix}";
    private const string BoolGetsOrInitializesPrefix = $"{GetsOrInitializesPrefix} {BoolValuePrefix}";

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options)
    {
        var seenSummary = false;
        var displayName = symbol.GetDisplayName();
        var minimallyQualifiedName = symbol.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

        foreach (var summaryElement in documentation.GetElements(DocumentationTags.Summary, recursive: true))
        {
            var location = summaryElement.GetLocation();
            bool isTopLevel = documentation.IsTopLevel(summaryElement);

            if (!isTopLevel || seenSummary)
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StraySummaryDocumentationRule, location, displayName));
                continue;
            }

            seenSummary = true;

            if (!QualityAnalyzer.IsLowQualityForAnyFormat(summaryElement, displayName, minimallyQualifiedName, options, DocumentationTags.Summary))
            {
                AnalyzePropertySummaryPattern(context, symbol, summaryElement, location, options);
                continue;
            }

            QualityAnalyzer.Report(context, location, DocumentationTags.Summary, displayName);
            AnalyzePropertySummaryPattern(context, symbol, summaryElement, location, options);
        }
    }

    private static void AnalyzePropertySummaryPattern(SymbolAnalysisContext context, ISymbol symbol, XmlNodeSyntax summaryElement, Location location, CommentSenseOptions options)
    {
        if (!options.RequirePropertyPatterns || symbol is not IPropertySymbol property)
            return;

        var expectedPrefix = GetExpectedPropertyPrefix(property);
        var disallowOrContinuation = expectedPrefix is GetsPrefix or SetsPrefix;
        var summaryText = summaryElement.GetInnerText().AsSpan().TrimStart();
        if (summaryText.IsEmpty || StartsWithPattern(summaryText, expectedPrefix.AsSpan(), disallowOrContinuation))
            return;

        context.ReportDiagnostic(Diagnostic.Create(
            CommentSenseRules.PropertySummaryPatternRule,
            location,
            property.GetDisplayName(),
            expectedPrefix));
    }

    private static string GetExpectedPropertyPrefix(IPropertySymbol property)
    {
        var getMethod = property.GetMethod;
        var setMethod = property.SetMethod;
        var hasVisibleGetter = IsVisibleAtPropertyScope(property, getMethod, out _);

        var isBoolean = property.Type.SpecialType == SpecialType.System_Boolean;
        if (hasVisibleGetter && IsVisibleAtPropertyScope(property, setMethod, out var visibleSetter))
        {
            var hasInitSetter = visibleSetter.IsInitOnly;
            return (isBoolean, hasInitSetter) switch
            {
                (true, true) => BoolGetsOrInitializesPrefix,
                (true, false) => BoolGetsOrSetsPrefix,
                (false, true) => GetsOrInitializesPrefix,
                (false, false) => GetsOrSetsPrefix
            };
        }

        return (isBoolean, hasVisibleGetter) switch
        {
            (true, true) => BoolGetsPrefix,
            (true, false) => BoolSetsPrefix,
            (false, true) => GetsPrefix,
            (false, false) => SetsPrefix
        };
    }

    private static bool IsVisibleAtPropertyScope(
        IPropertySymbol property,
        IMethodSymbol? accessor,
        [NotNullWhen(true)] out IMethodSymbol? visibleAccessor)
    {
        if (accessor is null)
        {
            visibleAccessor = null;
            return false;
        }

        var isGetter = SymbolEqualityComparer.Default.Equals(accessor, property.GetMethod);
        var isVisibleAtPropertyScope = !property.DeclaringSyntaxReferences
            .Select(reference => reference.GetSyntax())
            .OfType<BasePropertyDeclarationSyntax>()
            .SelectMany(baseProperty => baseProperty.AccessorList?.Accessors ?? [])
            .Where(accessorSyntax => IsMatchingAccessor(accessorSyntax, isGetter))
            .Any(accessorSyntax => accessorSyntax.Modifiers.Count > 0);

        visibleAccessor = isVisibleAtPropertyScope ? accessor : null;
        return isVisibleAtPropertyScope;
    }

    private static bool IsMatchingAccessor(AccessorDeclarationSyntax accessor, bool isGetter)
    {
        if (isGetter)
            return accessor.IsKind(SyntaxKind.GetAccessorDeclaration);

        return accessor.IsKind(SyntaxKind.SetAccessorDeclaration) || accessor.IsKind(SyntaxKind.InitAccessorDeclaration);
    }

    private static bool StartsWithPattern(ReadOnlySpan<char> text, ReadOnlySpan<char> expected, bool disallowOrContinuation)
    {
        var textIndex = 0;
        var expectedIndex = 0;

        while (expectedIndex < expected.Length)
        {
            if (expected[expectedIndex] == ' ')
            {
                if (!TryConsumeWhitespace(text, expected, ref textIndex, ref expectedIndex))
                    return false;

                continue;
            }

            if (!TryConsumeCaseInsensitiveCharacter(text, expected, ref textIndex, ref expectedIndex))
                return false;
        }

        return HasValidPatternRemainder(text, textIndex, disallowOrContinuation);
    }

    private static bool TryConsumeWhitespace(ReadOnlySpan<char> text, ReadOnlySpan<char> expected, ref int textIndex, ref int expectedIndex)
    {
        if (!IsWhitespaceAt(text, textIndex))
            return false;

        expectedIndex = SkipExpectedSpaces(expected, expectedIndex);
        textIndex = SkipTextWhitespace(text, textIndex);
        return true;
    }

    private static bool IsWhitespaceAt(ReadOnlySpan<char> text, int index)
    {
        return (uint)index < (uint)text.Length && char.IsWhiteSpace(text[index]);
    }

    private static int SkipExpectedSpaces(ReadOnlySpan<char> expected, int expectedIndex)
    {
        while ((uint)expectedIndex < (uint)expected.Length && expected[expectedIndex] == ' ')
        {
            expectedIndex++;
        }

        return expectedIndex;
    }

    private static int SkipTextWhitespace(ReadOnlySpan<char> text, int textIndex)
    {
        while ((uint)textIndex < (uint)text.Length && char.IsWhiteSpace(text[textIndex]))
        {
            textIndex++;
        }

        return textIndex;
    }

    private static bool TryConsumeCaseInsensitiveCharacter(ReadOnlySpan<char> text, ReadOnlySpan<char> expected, ref int textIndex, ref int expectedIndex)
    {
        if ((uint)textIndex >= (uint)text.Length)
            return false;

        if (char.ToUpperInvariant(text[textIndex]) != char.ToUpperInvariant(expected[expectedIndex]))
            return false;

        textIndex++;
        expectedIndex++;
        return true;
    }

    private static bool HasValidPatternRemainder(ReadOnlySpan<char> text, int textIndex, bool disallowOrContinuation)
    {
        if ((uint)textIndex < (uint)text.Length && char.IsLetterOrDigit(text[textIndex]))
            return false;

        if (!disallowOrContinuation)
            return true;

        var remainder = text.Slice(textIndex).TrimStart();
        return !remainder.StartsWith("or ".AsSpan(), StringComparison.OrdinalIgnoreCase);
    }
}
