using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class ExceptionAnalyzer
{
    private static readonly SymbolDisplayFormat FullNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    internal static string ToCrefString(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(FullNameFormat).Replace('<', '{').Replace('>', '}');
    }

    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>>> CompilationExceptionCache = new();
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, InheritedExceptionResolution>> CompilationInheritDocExceptionCache = new();
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<string, ExceptionTypeResolution>> CompilationExceptionFallbackCache = new();
    private static readonly ImmutableHashSet<ITypeSymbol> EmptyExceptionTypeSet = ImmutableHashSet.Create<ITypeSymbol>(SymbolEqualityComparer.Default);
    private static readonly InheritedExceptionResolution EmptyInheritedExceptionResolution = new(EmptyExceptionTypeSet, HasUnknownInclude: false);

    private readonly record struct EffectiveDocumentedExceptions(HashSet<ITypeSymbol> Types, bool HasUnknownInheritedDocumentation);
    private readonly record struct InheritedExceptionResolution(ImmutableHashSet<ITypeSymbol> Types, bool HasUnknownInclude);
    private readonly record struct ExceptionTypeResolution(ITypeSymbol? Type);

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options, bool isPrimaryCtor = false)
    {
        var effectiveDocumentation = GetEffectiveDocumentedExceptionTypes(context, symbol, documentation);
        var thrownTypes = GetThrownTypes(context, symbol, isPrimaryCtor, options);

        ReportMissingExceptions(context, symbol, documentation, options, thrownTypes, effectiveDocumentation);
        ReportLowQualityExceptions(context, documentation, options);
    }

    private static void ReportMissingExceptions(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation, CommentSenseOptions options, IEnumerable<ITypeSymbol> thrownTypes, EffectiveDocumentedExceptions effectiveDocumentation)
    {
        if (HasTopLevelIncludeTag(documentation) || effectiveDocumentation.HasUnknownInheritedDocumentation)
            return;

        var documentedTypes = effectiveDocumentation.Types;
        var filteredThrownTypes = new List<ITypeSymbol>();
        foreach (var thrownType in thrownTypes)
        {
            if (IsIgnored(thrownType, options))
                continue;

            if (!IsExceptionDocumented(thrownType, documentedTypes))
                filteredThrownTypes.Add(thrownType);
        }

        if (filteredThrownTypes.Count == 0)
            return;

        var sortedThrownTypes = filteredThrownTypes
            .Select(t => (Symbol: t, Cref: t.ToCrefString()))
            .OrderBy(x => x.Cref);

        foreach (var (thrownType, crefValue) in sortedThrownTypes)
        {
            var location = symbol.Locations.GetPrimaryLocation();
            var displayName = thrownType.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            var properties = ImmutableDictionary<string, string?>.Empty.Add(DocumentationAttributes.CrefProperty, crefValue);
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.MissingExceptionDocumentationRule, location, properties, displayName));
        }
    }

    private static bool IsExceptionDocumented(ITypeSymbol thrownType, HashSet<ITypeSymbol> documentedTypes)
    {
        if (documentedTypes.Contains(thrownType))
            return true;

        foreach (var documentedType in documentedTypes)
        {
            if (thrownType.InheritsFromOrEquals(documentedType))
                return true;
        }

        return false;
    }

    private static void ReportLowQualityExceptions(SymbolAnalysisContext context, DocumentationComment documentation, CommentSenseOptions options)
    {
        var seenExceptions = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var seenUnresolvedCrefs = new HashSet<string>(StringComparer.Ordinal);

        foreach (var exceptionElement in documentation.GetElements(DocumentationTags.Exception, recursive: true))
        {
            ReportExceptionElement(context, documentation, exceptionElement, options, seenExceptions, seenUnresolvedCrefs);
        }
    }

    private static void ReportExceptionElement(
        SymbolAnalysisContext context,
        DocumentationComment documentation,
        XmlNodeSyntax exceptionElement,
        CommentSenseOptions options,
        HashSet<ITypeSymbol> seenExceptions,
        HashSet<string> seenUnresolvedCrefs)
    {
        var location = exceptionElement.GetLocation();
        var cref = exceptionElement.GetAttributeValue(DocumentationAttributes.Cref);

        if (!documentation.IsTopLevel(exceptionElement))
        {
            var strayDisplayName = string.IsNullOrWhiteSpace(cref) ? "<unknown>" : cref;
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, strayDisplayName));
            return;
        }

        var resolved = ResolveExceptionType(exceptionElement, context.Compilation, context.CancellationToken);
        var displayName = resolved?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? cref ?? "<unknown>";

        if (resolved == null)
        {
            if (cref != null && !seenUnresolvedCrefs.Add(cref))
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, displayName));

            return;
        }

        if (seenExceptions.Add(resolved))
        {
            if (QualityAnalyzer.IsLowQuality(exceptionElement, resolved.Name, options, tagName: DocumentationTags.Exception))
                QualityAnalyzer.Report(context, location, DocumentationTags.Exception, resolved.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
        }
        else
        {
            context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, displayName));
        }
    }

    internal static bool IsIgnored(ITypeSymbol type, CommentSenseOptions options)
    {
        if (options.IgnoredExceptions.Contains(type.Name))
            return true;

        var fullName = type.ToDisplayString(FullNameFormat);
        if (options.IgnoredExceptions.Contains(fullName))
            return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named)
        {
            var originalFullName = named.OriginalDefinition.ToDisplayString(FullNameFormat);
            if (options.IgnoredExceptions.Contains(originalFullName))
                return true;
        }

        var nsSymbol = type.ContainingNamespace;
        if (nsSymbol is null || nsSymbol.IsGlobalNamespace)
            return false;

        var ns = nsSymbol.ToDisplayString();
        if (options.IgnoreSystemExceptions && IsInNamespace(ns, "System"))
            return true;

        foreach (var targetNs in options.IgnoredExceptionNamespaces)
        {
            if (IsInNamespace(ns, targetNs))
                return true;
        }

        return false;
    }

    internal static bool IsInNamespace(string ns, string targetNamespace)
    {
        if (ns.Equals(targetNamespace, StringComparison.OrdinalIgnoreCase))
            return true;

        if (ns.Length <= targetNamespace.Length)
            return false;

        return ns.StartsWith(targetNamespace, StringComparison.OrdinalIgnoreCase) && ns[targetNamespace.Length] == '.';
    }

    private static bool HasTopLevelIncludeTag(DocumentationComment documentation)
    {
        return documentation.GetElements(DocumentationTags.Include, recursive: false).Any();
    }

    private static bool HasTopLevelIncludeTag(XElement documentation)
    {
        return DocumentationXmlExtensions.GetTargetElements(documentation, DocumentationTags.Include, recursive: false).Any();
    }

    private static EffectiveDocumentedExceptions GetEffectiveDocumentedExceptionTypes(SymbolAnalysisContext context, ISymbol symbol, DocumentationComment documentation)
    {
        var documentedTypes = GetDocumentedExceptionTypes(
            context.Compilation,
            documentation,
            context.CancellationToken);

        if (!HasTopLevelInheritDoc(documentation))
            return new EffectiveDocumentedExceptions(documentedTypes, HasUnknownInheritedDocumentation: false);

        var recursionStack = new HashSet<ISymbol>(SymbolEqualityComparer.Default)
        {
            symbol
        };
        bool hasUnknownInheritedDocumentation = false;

        foreach (var target in GetInheritDocTargets(context.Compilation, symbol, context.CancellationToken))
        {
            var inheritedResolution = GetEffectiveExceptionTypesFromSymbol(target, context.Compilation, recursionStack, context.CancellationToken);
            documentedTypes.UnionWith(inheritedResolution.Types);
            hasUnknownInheritedDocumentation |= inheritedResolution.HasUnknownInclude;
        }

        return new EffectiveDocumentedExceptions(documentedTypes, hasUnknownInheritedDocumentation);
    }

    private static InheritedExceptionResolution GetEffectiveExceptionTypesFromSymbol(
        ISymbol symbol,
        Compilation compilation,
        HashSet<ISymbol> recursionStack,
        CancellationToken cancellationToken)
    {
        var cache = CompilationInheritDocExceptionCache.GetValue(
            compilation,
            _ => new ConcurrentDictionary<ISymbol, InheritedExceptionResolution>(SymbolEqualityComparer.Default));

        if (cache.TryGetValue(symbol, out var cached))
            return cached;

        if (!recursionStack.Add(symbol))
            return EmptyInheritedExceptionResolution;

        try
        {
            var documentation = DocumentationComment.FromSymbol(symbol, cancellationToken);
            if (documentation is not null && !documentation.IsMalformedFor(symbol, cancellationToken))
            {
                var syntaxResolution = GetEffectiveExceptionTypesFromDocumentation(
                    symbol,
                    compilation,
                    recursionStack,
                    documentation,
                    cancellationToken);
                cache.TryAdd(symbol, syntaxResolution);
                return syntaxResolution;
            }

            if (!DocumentationXmlExtensions.TryParseDocumentation(symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken), out var xmlDocumentation))
            {
                cache.TryAdd(symbol, EmptyInheritedExceptionResolution);
                return EmptyInheritedExceptionResolution;
            }

            var result = GetEffectiveExceptionTypesFromDocumentation(
                symbol,
                compilation,
                recursionStack,
                xmlDocumentation,
                cancellationToken);
            cache.TryAdd(symbol, result);
            return result;
        }
        finally
        {
            recursionStack.Remove(symbol);
        }
    }

    private static bool HasTopLevelInheritDoc(DocumentationComment documentation)
    {
        return documentation.GetElements(DocumentationTags.InheritDoc, recursive: false).Any();
    }

    private static bool HasTopLevelInheritDoc(XElement documentation)
    {
        return DocumentationXmlExtensions.GetTargetElements(documentation, DocumentationTags.InheritDoc, recursive: false).Any();
    }

    private static InheritedExceptionResolution GetEffectiveExceptionTypesFromDocumentation(
        ISymbol symbol,
        Compilation compilation,
        HashSet<ISymbol> recursionStack,
        DocumentationComment documentation,
        CancellationToken cancellationToken)
    {
        var documentedTypes = GetDocumentedExceptionTypes(compilation, documentation, cancellationToken);
        return CompleteInheritedExceptionResolution(symbol, compilation, recursionStack, documentedTypes, HasTopLevelIncludeTag(documentation), HasTopLevelInheritDoc(documentation), cancellationToken);
    }

    private static InheritedExceptionResolution GetEffectiveExceptionTypesFromDocumentation(
        ISymbol symbol,
        Compilation compilation,
        HashSet<ISymbol> recursionStack,
        XElement documentation,
        CancellationToken cancellationToken)
    {
        var documentedTypes = GetDocumentedExceptionTypes(compilation, documentation, cancellationToken);
        return CompleteInheritedExceptionResolution(symbol, compilation, recursionStack, documentedTypes, HasTopLevelIncludeTag(documentation), HasTopLevelInheritDoc(documentation), cancellationToken);
    }

    private static InheritedExceptionResolution CompleteInheritedExceptionResolution(
        ISymbol symbol,
        Compilation compilation,
        HashSet<ISymbol> recursionStack,
        HashSet<ITypeSymbol> documentedTypes,
        bool hasUnknownInclude,
        bool hasTopLevelInheritDoc,
        CancellationToken cancellationToken)
    {
        if (hasTopLevelInheritDoc)
        {
            var inheritDocTargets = GetInheritDocTargets(compilation, symbol, cancellationToken);
            if (inheritDocTargets.IsEmpty && !HasResolvableDeclaringSyntaxReference(compilation, symbol))
                hasUnknownInclude = true;

            foreach (var target in inheritDocTargets)
            {
                var inheritedResolution = GetEffectiveExceptionTypesFromSymbol(target, compilation, recursionStack, cancellationToken);
                documentedTypes.UnionWith(inheritedResolution.Types);
                hasUnknownInclude |= inheritedResolution.HasUnknownInclude;
            }
        }

        return new InheritedExceptionResolution(
            documentedTypes.ToImmutableHashSet<ITypeSymbol>(SymbolEqualityComparer.Default),
            hasUnknownInclude);
    }

    private static ImmutableArray<ISymbol> GetInheritDocTargets(Compilation compilation, ISymbol symbol, CancellationToken cancellationToken)
    {
        var builder = ImmutableArray.CreateBuilder<ISymbol>();
        var seenTargets = new HashSet<ISymbol>(SymbolEqualityComparer.Default);
        ImmutableArray<ISymbol>? implicitTargets = null;

        foreach (var declaringReference in DocumentationComment.GetDeclaringSyntaxReferences(symbol))
        {
            AddInheritDocTargetsFromDeclaration(
                compilation,
                symbol,
                declaringReference,
                builder,
                seenTargets,
                ref implicitTargets,
                cancellationToken);
        }

        return builder.ToImmutable();
    }

    private static bool HasResolvableDeclaringSyntaxReference(Compilation compilation, ISymbol symbol)
    {
        foreach (var declaringReference in DocumentationComment.GetDeclaringSyntaxReferences(symbol))
        {
            if (compilation.ContainsSyntaxTree(declaringReference.SyntaxTree))
                return true;
        }

        return false;
    }

    private static void AddInheritDocTargetsFromDeclaration(
        Compilation compilation,
        ISymbol symbol,
        SyntaxReference declaringReference,
        ImmutableArray<ISymbol>.Builder builder,
        HashSet<ISymbol> seenTargets,
        ref ImmutableArray<ISymbol>? implicitTargets,
        CancellationToken cancellationToken)
    {
        if (!compilation.ContainsSyntaxTree(declaringReference.SyntaxTree))
            return;

        var syntax = declaringReference.GetSyntax(cancellationToken);
        var docTrivia = DocumentationLocationExtensions.GetDocumentationCommentTrivia(syntax);
        if (docTrivia is null)
            return;

        var semanticModel = compilation.GetSemanticModel(declaringReference.SyntaxTree);
        foreach (var node in docTrivia.Content)
        {
            if (!InheritDocAnalyzer.TryGetInheritDocNode(node, out var crefAttribute))
                continue;

            if (crefAttribute is null)
            {
                implicitTargets ??= InheritDocAnalyzer.GetImplicitTargetsForInheritDoc(symbol);
                AddDistinctTargets(implicitTargets.Value, builder, seenTargets);
                continue;
            }

            var crefTarget = semanticModel.GetSymbolInfo(crefAttribute.Cref, cancellationToken).Symbol;
            AddDistinctTarget(crefTarget, builder, seenTargets);
        }
    }

    private static void AddDistinctTargets(
        ImmutableArray<ISymbol> targets,
        ImmutableArray<ISymbol>.Builder builder,
        HashSet<ISymbol> seenTargets)
    {
        foreach (var target in targets)
        {
            AddDistinctTarget(target, builder, seenTargets);
        }
    }

    private static void AddDistinctTarget(
        ISymbol? target,
        ImmutableArray<ISymbol>.Builder builder,
        HashSet<ISymbol> seenTargets)
    {
        if (target is not null && seenTargets.Add(target))
            builder.Add(target);
    }

    private static HashSet<ITypeSymbol> GetDocumentedExceptionTypes(Compilation compilation, IEnumerable<string> crefs, CancellationToken cancellationToken = default)
    {
        var documentedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var cref in crefs)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (ResolveExceptionType(cref, compilation, cancellationToken) is { } resolved)
                documentedTypes.Add(resolved);
        }

        return documentedTypes;
    }

    private static HashSet<ITypeSymbol> GetDocumentedExceptionTypes(Compilation compilation, DocumentationComment documentation, CancellationToken cancellationToken = default)
    {
        var documentedTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        foreach (var exceptionElement in documentation.GetElements(DocumentationTags.Exception, recursive: false))
        {
            cancellationToken.ThrowIfCancellationRequested();
            foreach (var resolved in ResolveExceptionTypes(exceptionElement, compilation, cancellationToken))
                documentedTypes.Add(resolved);
        }

        return documentedTypes;
    }

    private static HashSet<ITypeSymbol> GetDocumentedExceptionTypes(Compilation compilation, XElement documentation, CancellationToken cancellationToken = default)
    {
        return GetDocumentedExceptionTypes(
            compilation,
            DocumentationXmlExtensions.GetExceptionCrefs(documentation),
            cancellationToken);
    }

    private sealed record CrefInfo(char? Prefix, string TypeName, string OriginalCref)
    {
        public static CrefInfo Parse(string cref)
        {
            if (cref.Length >= 2 && cref[1] == ':')
                return new CrefInfo(cref[0], cref.Substring(2), cref);

            return new CrefInfo(null, cref, cref);
        }

        public string DocId
        {
            get
            {
                if (Prefix.HasValue)
                    return OriginalCref;

                return "T:" + OriginalCref;
            }
        }

        public bool IsPotentiallyValidException => Prefix switch
        {
            null or 'T' or '!' => true,
            _ => false
        };
    }

    internal static ITypeSymbol? ResolveExceptionType(string? cref, Compilation compilation, CancellationToken cancellationToken = default)
    {
        if (cref == null || string.IsNullOrWhiteSpace(cref))
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        cref = cref.Trim();
        var info = CrefInfo.Parse(cref);

        var resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(info.DocId, compilation);
        if (resolved is ITypeSymbol ts)
            return ts;

        var fallbackCache = CompilationExceptionFallbackCache.GetValue(
            compilation,
            _ => new ConcurrentDictionary<string, ExceptionTypeResolution>(StringComparer.Ordinal));

        var cachedResult = fallbackCache.GetOrAdd(
            cref,
            _ => new ExceptionTypeResolution(ResolveExceptionTypeFallback(info, compilation, cancellationToken)));

        return cachedResult.Type;
    }

    private static ITypeSymbol? ResolveExceptionType(XmlNodeSyntax exceptionElement, Compilation compilation, CancellationToken cancellationToken)
    {
        var syntaxResolved = TryResolveExceptionTypeFromSyntax(exceptionElement, compilation, cancellationToken);
        if (syntaxResolved is not null)
            return syntaxResolved;

        return ResolveExceptionType(exceptionElement.GetAttributeValue(DocumentationAttributes.Cref), compilation, cancellationToken);
    }

    private static IEnumerable<ITypeSymbol> ResolveExceptionTypes(XmlNodeSyntax exceptionElement, Compilation compilation, CancellationToken cancellationToken)
    {
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);

        if (TryResolveExceptionTypeFromSyntax(exceptionElement, compilation, cancellationToken) is { } syntaxResolved &&
            seen.Add(syntaxResolved))
        {
            yield return syntaxResolved;
        }

        if (ResolveExceptionType(exceptionElement.GetAttributeValue(DocumentationAttributes.Cref), compilation, cancellationToken) is { } fallbackResolved &&
            seen.Add(fallbackResolved))
        {
            yield return fallbackResolved;
        }
    }

    private static ITypeSymbol? TryResolveExceptionTypeFromSyntax(XmlNodeSyntax exceptionElement, Compilation compilation, CancellationToken cancellationToken)
    {
        var crefSyntax = GetCrefSyntax(exceptionElement);
        if (crefSyntax == null || !compilation.ContainsSyntaxTree(exceptionElement.SyntaxTree))
            return null;

        var semanticModel = compilation.GetSemanticModel(exceptionElement.SyntaxTree);
        var symbolInfo = semanticModel.GetSymbolInfo(crefSyntax, cancellationToken);
        var resolvedSymbol = symbolInfo.Symbol ?? symbolInfo.CandidateSymbols.FirstOrDefault();

        return resolvedSymbol as ITypeSymbol;
    }

    private static CrefSyntax? GetCrefSyntax(XmlNodeSyntax exceptionElement)
    {
        var attributes = exceptionElement switch
        {
            XmlElementSyntax element => element.StartTag.Attributes,
            XmlEmptyElementSyntax emptyElement => emptyElement.Attributes,
            _ => default
        };

        foreach (var attribute in attributes)
        {
            if (attribute is XmlCrefAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Cref } crefAttribute)
                return crefAttribute.Cref;
        }

        return null;
    }

    private static ITypeSymbol? ResolveExceptionTypeFallback(CrefInfo info, Compilation compilation, CancellationToken cancellationToken)
    {
        if (!info.IsPotentiallyValidException)
            return null;

        cancellationToken.ThrowIfCancellationRequested();
        var normalizedTypeName = StripGlobalAlias(DocumentationSyntaxExtensions.NormalizeCref(info.TypeName));
        var typeNameWithoutGenerics = RemoveGenerics(normalizedTypeName.AsSpan());

        if (TryResolveByMetadataName(compilation, normalizedTypeName, typeNameWithoutGenerics, cancellationToken) is { } metadataType)
            return metadataType;

        // Extract simple name of the target type to use fast lookup (ignoring generic arguments)
        var nameSpan = typeNameWithoutGenerics.AsSpan();
        int lastDotIndex = nameSpan.LastIndexOf('.');
        bool isUnqualifiedName = lastDotIndex == -1;
        var lastPart = lastDotIndex == -1 ? nameSpan : nameSpan.Slice(lastDotIndex + 1);
        var simpleNameSpan = lastPart;

        var simpleName = simpleNameSpan.ToString();

        if (string.IsNullOrWhiteSpace(simpleName) || !SyntaxFacts.IsValidIdentifier(simpleName))
            return null;

        var symbols = GetSymbolsByName(compilation, simpleName, cancellationToken);
        var resolved = FindBestExceptionMatch(symbols, normalizedTypeName, typeNameWithoutGenerics, cancellationToken);
        if (resolved != null)
            return resolved;

        return isUnqualifiedName
            ? TryResolveSystemTypeBySimpleName(compilation, simpleName, cancellationToken)
            : null;
    }

    private static ITypeSymbol? TryResolveByMetadataName(Compilation compilation, string normalizedTypeName, string typeNameWithoutGenerics, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var metadataCandidate = StripGlobalAlias(normalizedTypeName);
        if (!metadataCandidate.Contains('<'))
        {
            var directMatch = compilation.GetTypeByMetadataName(metadataCandidate);
            if (directMatch != null)
                return directMatch;
        }

        var simpleCandidate = StripGlobalAlias(typeNameWithoutGenerics);
        if (!simpleCandidate.Equals(metadataCandidate, StringComparison.Ordinal))
        {
            var simpleMatch = compilation.GetTypeByMetadataName(simpleCandidate);
            if (simpleMatch != null)
                return simpleMatch;
        }

        var genericCandidate = TryBuildGenericMetadataName(metadataCandidate, cancellationToken);
        if (genericCandidate == null)
            return null;

        return compilation.GetTypeByMetadataName(genericCandidate);
    }

    private static string StripGlobalAlias(string typeName)
    {
        const string globalAliasPrefix = "global::";
        return typeName.StartsWith(globalAliasPrefix, StringComparison.Ordinal)
            ? typeName.Substring(globalAliasPrefix.Length)
            : typeName;
    }

    private static string? TryBuildGenericMetadataName(string typeName, CancellationToken cancellationToken)
    {
        if (typeName.IndexOf('<') <= 0)
            return null;

        var metadataName = new System.Text.StringBuilder(typeName.Length);
        int index = 0;
        while (index < typeName.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();
            char current = typeName[index];
            if (current != '<')
            {
                metadataName.Append(current);
                index++;
                continue;
            }

            if (!TryFindGenericArity(typeName, index, cancellationToken, out int arity, out int closingBracketIndex))
                return null;

            metadataName.Append('`');
            metadataName.Append(arity);
            index = closingBracketIndex + 1;
        }

        return metadataName.ToString();
    }

    private static bool TryFindGenericArity(string typeName, int openingBracketIndex, CancellationToken cancellationToken, out int arity, out int closingBracketIndex)
    {
        int depth = 0;
        arity = 1;
        closingBracketIndex = -1;

        for (int i = openingBracketIndex + 1; i < typeName.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            switch (typeName[i])
            {
                case '<':
                    depth++;
                    break;
                case '>':
                    if (depth == 0)
                    {
                        closingBracketIndex = i;
                        return true;
                    }

                    depth--;
                    break;
                case ',' when depth == 0:
                    arity++;
                    break;
            }
        }

        return false;
    }

    private static ITypeSymbol? TryResolveSystemTypeBySimpleName(Compilation compilation, string simpleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return compilation.GetTypeByMetadataName("System." + simpleName);
    }

    private static List<ITypeSymbol> GetSymbolsByName(Compilation compilation, string simpleName, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Use Roslyn's name index and prioritize current assembly symbols first.
        var indexedSymbols = compilation.GetSymbolsWithName(simpleName, SymbolFilter.Type, cancellationToken)
            .Where(s => s.Name.Equals(simpleName, StringComparison.Ordinal))
            .OfType<ITypeSymbol>()
            .Where(t => !t.IsImplicitlyDeclared)
            .ToList();

        if (indexedSymbols.Count == 0)
            return indexedSymbols;

        return indexedSymbols
            .OrderByDescending(symbol => SymbolEqualityComparer.Default.Equals(symbol.ContainingAssembly, compilation.Assembly))
            .ToList();
    }

    private static ITypeSymbol? FindBestExceptionMatch(List<ITypeSymbol> symbols, string normalizedTypeName, string typeNameWithoutGenerics, CancellationToken cancellationToken)
    {
        ITypeSymbol? genericFallback = null;

        foreach (var t in symbols)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var fullName = t.ToDisplayString(FullNameFormat);
            var minName = t.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);
            if (fullName == normalizedTypeName || minName == normalizedTypeName || t.Name == normalizedTypeName)
                return t;

            if (genericFallback != null)
                continue;

            if (t is not INamedTypeSymbol { IsGenericType: true } named)
                continue;

            var definitionFull = RemoveGenerics(named.OriginalDefinition.ToDisplayString(FullNameFormat).AsSpan());
            var definitionMin = named.OriginalDefinition.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat);

            if (definitionMin.Contains('<'))
                definitionMin = definitionMin.Substring(0, definitionMin.IndexOf('<'));

            if (definitionFull == typeNameWithoutGenerics || definitionMin == typeNameWithoutGenerics)
                genericFallback = t;
        }

        return genericFallback;
    }

    private static string RemoveGenerics(ReadOnlySpan<char> span)
    {
        if (span.IndexOfAny('<', '{') == -1)
            return span.ToString();

        var result = new System.Text.StringBuilder(span.Length);
        int depth = 0;
        foreach (char c in span)
        {
            switch (c)
            {
                case '<':
                case '{':
                    depth++;
                    break;
                case '>':
                case '}':
                    depth--;
                    break;
                default:
                    {
                        if (depth == 0)
                            result.Append(c);
                        break;
                    }
            }
        }

        return result.ToString();
    }

    internal static string? FindBestMatchingThrownException(ISymbol symbol, string crefText, CommentSenseOptions options, Compilation compilation, CancellationToken cancellationToken = default)
    {
        if (options.RenameSimilarityThreshold <= 0.0)
            return null;

        var isPrimaryCtor = symbol.IsPrimaryConstructor();
        var thrownTypes = GetThrownTypes(compilation, symbol, isPrimaryCtor, options, cancellationToken);
        if (thrownTypes.Count == 0)
            return null;

        var simpleCrefName = GetSimpleCrefName(crefText);

        // 1. Exact name match
        foreach (var t in thrownTypes)
        {
            if (t.Name.Equals(simpleCrefName, StringComparison.OrdinalIgnoreCase))
                return t.ToCrefString();
        }

        // 2. If only one exception is thrown, suggest it if there's any similarity
        if (thrownTypes.Count == 1)
        {
            var single = thrownTypes.First();
            if (IsSingleExceptionMatch(single, simpleCrefName, options.RenameSimilarityThreshold))
            {
                return single.ToCrefString();
            }
        }

        // 3. Fuzzy match by name similarity
        return FindFuzzyExceptionMatch(thrownTypes, simpleCrefName, options.RenameSimilarityThreshold);
    }

    private static string GetSimpleCrefName(string crefText)
    {
        var normalizedCref = DocumentationSyntaxExtensions.NormalizeCref(crefText);
        var crefSpan = normalizedCref.AsSpan();
        int lastDotIndex = crefSpan.LastIndexOf('.');
        var lastPart = lastDotIndex == -1 ? crefSpan : crefSpan.Slice(lastDotIndex + 1);
        return RemoveGenerics(lastPart);
    }

    private static bool IsSingleExceptionMatch(ITypeSymbol single, string simpleCrefName, double threshold)
    {
        if (simpleCrefName.Length > 2 && (single.Name.IndexOf(simpleCrefName, StringComparison.OrdinalIgnoreCase) >= 0 ||
             simpleCrefName.IndexOf(single.Name, StringComparison.OrdinalIgnoreCase) >= 0))
        {
            return true;
        }

        return single.Name.CalculateSimilarity(simpleCrefName) >= threshold;
    }

    private static string? FindFuzzyExceptionMatch(HashSet<ITypeSymbol> thrownTypes, string simpleCrefName, double threshold)
    {
        ITypeSymbol? bestMatch = null;
        double bestSimilarity = -1.0;

        foreach (var t in thrownTypes)
        {
            double similarity = t.Name.CalculateSimilarity(simpleCrefName);
            if (similarity < threshold || similarity <= bestSimilarity)
                continue;

            bestSimilarity = similarity;
            bestMatch = t;
        }

        return bestMatch?.ToCrefString();
    }

    private static HashSet<ITypeSymbol> GetThrownTypes(Compilation compilation, ISymbol symbol, bool isPrimaryCtor, CommentSenseOptions options, CancellationToken cancellationToken = default)
    {
        var thrownTypes = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var exceptionCache = CompilationExceptionCache.GetValue(compilation, _ => new ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>>(SymbolEqualityComparer.Default));

        foreach (var syntaxReference in symbol.DeclaringSyntaxReferences)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

            var nodes = GetDescendantNodesOfInterest(syntax, isPrimaryCtor, options.ScanCalledMethodsForExceptions, cancellationToken);
            var exceptions = IdentifyThrownExceptions(nodes, semanticModel, options, exceptionCache, cancellationToken);

            thrownTypes.UnionWith(exceptions);
        }

        return thrownTypes;
    }

    private static HashSet<ITypeSymbol> GetThrownTypes(SymbolAnalysisContext context, ISymbol symbol, bool isPrimaryCtor, CommentSenseOptions options)
    {
        return GetThrownTypes(context.Compilation, symbol, isPrimaryCtor, options, context.CancellationToken);
    }

    private static IEnumerable<SyntaxNode> GetDescendantNodesOfInterest(SyntaxNode root, bool isPrimaryCtor, bool scanCalledMethods, CancellationToken cancellationToken)
    {
        foreach (var analysisRoot in GetAnalysisRoots(root, isPrimaryCtor))
        {
            foreach (var node in analysisRoot.DescendantNodesAndSelf(n => ShouldDescendIntoNode(analysisRoot, n, isPrimaryCtor)))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (IsNodeOfInterest(node, scanCalledMethods))
                    yield return node;
            }
        }
    }

    private static IEnumerable<SyntaxNode> GetAnalysisRoots(SyntaxNode root, bool isPrimaryCtor)
    {
        if (isPrimaryCtor)
            return EnumerateSingleRoot(root);

        return root switch
        {
            BaseMethodDeclarationSyntax methodDeclaration => GetMethodAnalysisRoots(methodDeclaration),
            PropertyDeclarationSyntax propertyDeclaration => GetPropertyAnalysisRoots(propertyDeclaration),
            IndexerDeclarationSyntax indexerDeclaration => GetIndexerAnalysisRoots(indexerDeclaration),
            _ => EnumerateSingleRoot(root)
        };
    }

    private static IEnumerable<SyntaxNode> EnumerateSingleRoot(SyntaxNode root)
    {
        yield return root;
    }

    private static IEnumerable<SyntaxNode> GetMethodAnalysisRoots(BaseMethodDeclarationSyntax methodDeclaration)
    {
        if (methodDeclaration is ConstructorDeclarationSyntax { Initializer: { } initializer })
            yield return initializer;

        if (methodDeclaration.ExpressionBody is { Expression: { } methodExpression })
            yield return methodExpression;

        if (methodDeclaration.Body is { } methodBody)
            yield return methodBody;
    }

    private static IEnumerable<SyntaxNode> GetPropertyAnalysisRoots(PropertyDeclarationSyntax propertyDeclaration)
    {
        if (propertyDeclaration.Initializer is { Value: { } propertyInitializer })
            yield return propertyInitializer;

        if (propertyDeclaration.ExpressionBody is { Expression: { } propertyExpression })
            yield return propertyExpression;

        if (propertyDeclaration.AccessorList is { } propertyAccessorList)
            yield return propertyAccessorList;
    }

    private static IEnumerable<SyntaxNode> GetIndexerAnalysisRoots(IndexerDeclarationSyntax indexerDeclaration)
    {
        if (indexerDeclaration.ExpressionBody is { Expression: { } indexerExpression })
            yield return indexerExpression;

        if (indexerDeclaration.AccessorList is { } indexerAccessorList)
            yield return indexerAccessorList;
    }

    private static bool ShouldDescendIntoNode(SyntaxNode analysisRoot, SyntaxNode node, bool isPrimaryCtor)
    {
        // Ensure we don't block the root node (ClassDeclaration is BaseTypeDeclaration)
        if (node == analysisRoot)
            return true;

        if (node is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseTypeDeclarationSyntax)
            return false;

        if (isPrimaryCtor && IsExcludedPrimaryConstructorMember(node))
            return false;

        return true;
    }

    private static bool IsNodeOfInterest(SyntaxNode node, bool scanCalledMethods)
    {
        return node is ThrowStatementSyntax
                    or ThrowExpressionSyntax
                    or InvocationExpressionSyntax
                    or ConstructorInitializerSyntax ||
               (scanCalledMethods && node is ObjectCreationExpressionSyntax
                    or ImplicitObjectCreationExpressionSyntax
                    or MemberAccessExpressionSyntax
                    or MemberBindingExpressionSyntax
                    or IdentifierNameSyntax
                    or ElementAccessExpressionSyntax
                    or ElementBindingExpressionSyntax);
    }

    private static bool IsExcludedPrimaryConstructorMember(SyntaxNode n)
    {
        // Block members that have their own analysis to avoid duplicates.
        // We descend into FieldDeclaration because fields don't have their own ExceptionAnalyzer.
        return n is MethodDeclarationSyntax
                    or ConstructorDeclarationSyntax
                    or PropertyDeclarationSyntax
                    or IndexerDeclarationSyntax
                    or AccessorListSyntax
                    or AccessorDeclarationSyntax
                    or EventDeclarationSyntax;
    }

    private static IEnumerable<ITypeSymbol> IdentifyThrownExceptions(IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel, CommentSenseOptions options, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        var exceptionType = semanticModel.Compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType is null)
            yield break;

        foreach (var node in nodes)
        {
            token.ThrowIfCancellationRequested();
            var exceptions = GetExceptionsFromNode(node, semanticModel, options, exceptionType, exceptionCache, token);
            foreach (var exception in exceptions)
            {
                token.ThrowIfCancellationRequested();
                if (exception != null && !IsCaughtLocally(node, exception, semanticModel))
                {
                    yield return exception;
                }
            }
        }
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromNode(SyntaxNode node, SemanticModel semanticModel, CommentSenseOptions options, ITypeSymbol exceptionType, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        return node switch
        {
            ThrowStatementSyntax ts => GetExceptionsFromThrowStatement(ts, semanticModel, exceptionType, token),
            ThrowExpressionSyntax te => GetExceptionsFromThrowExpression(te, semanticModel, token),
            InvocationExpressionSyntax invocation => GetExceptionsFromInvocationInternal(invocation, semanticModel, options, exceptionType, exceptionCache, token),
            ObjectCreationExpressionSyntax objectCreation => GetExceptionsFromObjectCreation(objectCreation, semanticModel, exceptionCache, token),
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => GetExceptionsFromImplicitObjectCreation(implicitObjectCreation, semanticModel, exceptionCache, token),
            ConstructorInitializerSyntax ci when options.ScanCalledMethodsForExceptions => GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(ci, token).Symbol, semanticModel.Compilation, exceptionCache, token),
            MemberAccessExpressionSyntax ma => GetExceptionsFromMemberAccess(ma, semanticModel, exceptionCache, token),
            MemberBindingExpressionSyntax mb => GetExceptionsFromMemberBinding(mb, semanticModel, exceptionCache, token),
            IdentifierNameSyntax id => GetExceptionsFromIdentifier(id, semanticModel, exceptionCache, token),
            ElementAccessExpressionSyntax elementAccess => GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(elementAccess, token).Symbol, semanticModel.Compilation, exceptionCache, token),
            ElementBindingExpressionSyntax eb => GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(eb, token).Symbol, semanticModel.Compilation, exceptionCache, token),
            _ => []
        };
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromThrowStatement(ThrowStatementSyntax ts, SemanticModel semanticModel, ITypeSymbol exceptionType, CancellationToken token)
    {
        return
        [
            ts.Expression is not null
                ? semanticModel.GetTypeInfo(ts.Expression, token).Type
                : GetCaughtExceptionType(ts, semanticModel, exceptionType, token)
        ];
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromThrowExpression(ThrowExpressionSyntax te, SemanticModel semanticModel, CancellationToken token)
    {
        return [semanticModel.GetTypeInfo(te.Expression, token).Type];
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromInvocationInternal(InvocationExpressionSyntax invocation, SemanticModel semanticModel, CommentSenseOptions options, ITypeSymbol exceptionType, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        if (options.ScanCalledMethodsForExceptions)
        {
            return GetExceptionsFromInvocation(invocation, semanticModel, exceptionType, exceptionCache, token);
        }

        var symbol = semanticModel.GetSymbolInfo(invocation, token).Symbol;
        return [GetExceptionTypeFromGuardClause(invocation, symbol, exceptionType)];
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromObjectCreation(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        return GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(objectCreation, token).Symbol, semanticModel.Compilation, exceptionCache, token);
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromImplicitObjectCreation(ImplicitObjectCreationExpressionSyntax implicitObjectCreation, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        return GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(implicitObjectCreation, token).Symbol, semanticModel.Compilation, exceptionCache, token);
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromMemberAccess(MemberAccessExpressionSyntax ma, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        // Only process if it's NOT the expression of an invocation (that's handled by InvocationExpressionSyntax)
        return ma.Parent is InvocationExpressionSyntax parentInvocation && parentInvocation.Expression == ma
            ? []
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(ma, token).Symbol, semanticModel.Compilation, exceptionCache, token);
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromMemberBinding(MemberBindingExpressionSyntax mb, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        // Only process if it's NOT the expression of an invocation (that's handled by InvocationExpressionSyntax)
        return mb.Parent is InvocationExpressionSyntax parentInvocationMb && parentInvocationMb.Expression == mb
            ? []
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(mb, token).Symbol, semanticModel.Compilation, exceptionCache, token);
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromIdentifier(IdentifierNameSyntax id, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        // Avoid redundant processing:
        // 1. If it's the Name of a MemberAccessExpression, the MemberAccessExpression itself handles the symbol.
        // 2. If it's the Expression of an InvocationExpression, the InvocationExpression handles it.
        var isRedundant = (id.Parent is MemberAccessExpressionSyntax maParent && maParent.Name == id) ||
                          (id.Parent is InvocationExpressionSyntax parentInvocation2 && parentInvocation2.Expression == id);

        return isRedundant
            ? []
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(id, token).Symbol, semanticModel.Compilation, exceptionCache, token);
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ITypeSymbol exceptionType, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation, token).Symbol;

        var guardException = GetExceptionTypeFromGuardClause(invocation, symbol, exceptionType);
        var exceptions = GetExceptionsFromSymbol(symbol, semanticModel.Compilation, exceptionCache, token);
        var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var candidates = exceptions
            .Cast<ITypeSymbol?>()
            .Append(guardException)
            .Where(static exception => exception is not null)
            .Cast<ITypeSymbol>();

        foreach (var exception in candidates)
        {
            token.ThrowIfCancellationRequested();
            if (seen.Add(exception))
                yield return exception;
        }
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromSymbol(ISymbol? symbol, Compilation compilation, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> cache, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        if (symbol is not (IMethodSymbol or IPropertySymbol or IEventSymbol))
            return [];

        return cache.GetOrAdd(symbol, s => [.. GetExceptionsFromSymbolInternal(s, compilation, token)]);
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromSymbolInternal(ISymbol symbol, Compilation compilation, CancellationToken token)
    {
        if (symbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } delegateMethod)
        {
            symbol = delegateMethod.ContainingType;
        }

        if (DocumentationComment.FromSymbol(symbol, token) is { } documentation && !documentation.IsMalformedFor(symbol, token))
        {
            var seen = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var exceptionElement in documentation.GetElements(DocumentationTags.Exception, recursive: false))
            {
                token.ThrowIfCancellationRequested();
                foreach (var resolved in ResolveExceptionTypes(exceptionElement, compilation, token))
                {
                    if (seen.Add(resolved))
                        yield return resolved;
                }
            }

            yield break;
        }

        foreach (var cref in DocumentationXmlExtensions.GetExceptionCrefs(symbol.GetDocumentationCommentXml(cancellationToken: token)))
        {
            token.ThrowIfCancellationRequested();
            if (ResolveExceptionType(cref, compilation, token) is { } resolved)
            {
                yield return resolved;
            }
        }
    }

    private static ITypeSymbol? GetExceptionTypeFromGuardClause(InvocationExpressionSyntax invocation, ISymbol? symbol, ITypeSymbol exceptionType)
    {
        var name = invocation.Expression switch
        {
            MemberAccessExpressionSyntax ma => ma.Name.Identifier.ValueText,
            IdentifierNameSyntax id => id.Identifier.ValueText,
            _ => null
        };

        if (name == null || !name.StartsWith("Throw", StringComparison.Ordinal))
            return null;

        if (symbol is not IMethodSymbol { IsStatic: true, ReturnsVoid: true } method)
            return null;

        if (method.ContainingType.InheritsFromOrEquals(exceptionType))
            return method.ContainingType;

        return null;
    }

    private static bool IsCaughtLocally(SyntaxNode throwNode, ITypeSymbol thrownType, SemanticModel semanticModel)
    {
        var current = throwNode.Parent;
        while (current != null)
        {
            switch (current)
            {
                case TryStatementSyntax tryStatement:
                    {
                        // Only consider it caught if the throw is inside the 'try' block
                        // (Exceptions thrown in catch/finally blocks escape this try statement)
                        if (tryStatement.Block.Span.Contains(throwNode.Span))
                        {
                            var isCaught = tryStatement.Catches
                                                       .Where(c => c.Filter == null)
                                                       .Any(c => c.Declaration == null ||
                                                                 (semanticModel.GetTypeInfo(c.Declaration.Type).Type is { } caughtType &&
                                                                  thrownType.InheritsFromOrEquals(caughtType)));

                            if (isCaught)
                                return true;
                        }

                        break;
                    }
                case MethodDeclarationSyntax:
                case LocalFunctionStatementSyntax:
                case ConstructorDeclarationSyntax:
                case AccessorDeclarationSyntax:
                    // Stop at method boundary
                    return false;
            }

            current = current.Parent;
        }

        return false;
    }

    private static ITypeSymbol? GetCaughtExceptionType(ThrowStatementSyntax throwStatement, SemanticModel semanticModel, ITypeSymbol? exceptionType, CancellationToken cancellationToken)
    {
        var catchClause = throwStatement.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause?.Declaration is null)
            return exceptionType;

        return semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
    }
}
