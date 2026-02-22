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
    internal static readonly SymbolDisplayFormat FullNameFormat = new(
        globalNamespaceStyle: SymbolDisplayGlobalNamespaceStyle.Omitted,
        typeQualificationStyle: SymbolDisplayTypeQualificationStyle.NameAndContainingTypesAndNamespaces,
        genericsOptions: SymbolDisplayGenericsOptions.IncludeTypeParameters);

    internal static string ToCrefString(this ITypeSymbol symbol)
    {
        return symbol.ToDisplayString(FullNameFormat).Replace('<', '{').Replace('>', '}');
    }

    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>>> CompilationExceptionCache = new();
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, IImmutableSet<ITypeSymbol>>> DocumentedExceptionCache = new();

    public static void Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options, bool isPrimaryCtor = false)
    {
        var documentedExceptionElements = DocumentationXmlExtensions.GetTargetElements(xml, DocumentationTags.Exception);
        var documentedTypes = GetDocumentedExceptionTypes(context, documentedExceptionElements);
        var thrownTypes = GetThrownTypes(context, symbol, isPrimaryCtor, options);

        ReportMissingExceptions(context, symbol, xml, options, thrownTypes, documentedTypes);
        ReportLowQualityExceptions(context, symbol, xml, options);
    }

    private static void ReportMissingExceptions(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options, IEnumerable<ITypeSymbol> thrownTypes, HashSet<ITypeSymbol> documentedTypes)
    {
        // CSENSE012: Missing Exception Documentation
        bool hasInheritDoc = DocumentationXmlExtensions.HasTopLevelInheritDoc(xml);

        if (DocumentationXmlExtensions.HasAutoValidTag(xml) && !hasInheritDoc)
            return;

        if (hasInheritDoc)
        {
            HashSet<ISymbol> visited = new(SymbolEqualityComparer.Default) { symbol };
            AddInheritedExceptions(xml, symbol, context.Compilation, documentedTypes, visited, context.CancellationToken);
        }

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

    private static void ReportLowQualityExceptions(SymbolAnalysisContext context, ISymbol symbol, XElement xml, CommentSenseOptions options)
    {
        // CSENSE016: Low Quality Exception Documentation
        // CSENSE023: Stray Exception Documentation
        var seenExceptions = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
        var seenUnresolvedCrefs = new HashSet<string>(StringComparer.Ordinal);
        var effectiveTarget = DocumentationXmlExtensions.GetEffectiveTarget(xml);

        foreach (var (exceptionElement, location) in symbol.GetTargetElementsWithLocations(xml, DocumentationTags.Exception, topLevelOnly: false))
        {
            var cref = exceptionElement.Attribute(DocumentationAttributes.Cref)?.Value;

            bool isTopLevel = DocumentationXmlExtensions.IsTopLevel(xml, exceptionElement, effectiveTarget);
            if (!isTopLevel)
            {
                var strayDisplayName = string.IsNullOrWhiteSpace(cref)
                    ? "<unknown>"
                    : cref;

                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, strayDisplayName));
                continue;
            }

            var resolved = ResolveExceptionType(cref, context.Compilation);
            var displayName = resolved?.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat) ?? cref ?? "<unknown>";

            if (resolved == null)
            {
                if (cref != null && !seenUnresolvedCrefs.Add(cref))
                    context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, displayName));

                continue;
            }

            if (seenExceptions.Add(resolved))
            {
                var isLowQuality = QualityAnalyzer.IsLowQuality(exceptionElement, resolved.Name, options, tagName: DocumentationTags.Exception);
                if (isLowQuality)
                    QualityAnalyzer.Report(context, location, DocumentationTags.Exception, resolved.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
            }
            else
            {
                context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.StrayExceptionDocumentationRule, location, displayName));
            }
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

    private static HashSet<ITypeSymbol> GetDocumentedExceptionTypes(SymbolAnalysisContext context, IEnumerable<XElement> exceptionElements)
    {
        return new HashSet<ITypeSymbol>(
            exceptionElements
                .Select(e => e.Attribute(DocumentationAttributes.Cref)?.Value)
                .Select(cref => ResolveExceptionType(cref, context.Compilation))
                .OfType<ITypeSymbol>(),
            SymbolEqualityComparer.Default);
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

    internal static ITypeSymbol? ResolveExceptionType(string? cref, Compilation compilation)
    {
        if (cref == null || string.IsNullOrWhiteSpace(cref))
            return null;

        cref = cref.Trim();
        var info = CrefInfo.Parse(cref);

        var resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(info.DocId, compilation);
        if (resolved is ITypeSymbol ts)
            return ts;

        return ResolveExceptionTypeFallback(info, compilation);
    }

    private static ITypeSymbol? ResolveExceptionTypeFallback(CrefInfo info, Compilation compilation)
    {
        if (!info.IsPotentiallyValidException)
            return null;

        var normalizedTypeName = DocumentationSyntaxExtensions.NormalizeCref(info.TypeName);
        var typeNameWithoutGenerics = RemoveGenerics(normalizedTypeName.AsSpan());

        // Extract simple name of the target type to use fast lookup (ignoring generic arguments)
        var nameSpan = normalizedTypeName.AsSpan();
        int lastDotIndex = nameSpan.LastIndexOf('.');
        var lastPart = lastDotIndex == -1 ? nameSpan : nameSpan.Slice(lastDotIndex + 1);
        int genericStartIndex = lastPart.IndexOf('<');
        var simpleNameSpan = genericStartIndex == -1 ? lastPart : lastPart.Slice(0, genericStartIndex);

        var simpleName = simpleNameSpan.ToString();

        if (string.IsNullOrWhiteSpace(simpleName) || !SyntaxFacts.IsValidIdentifier(simpleName))
            return null;

        // Try direct lookup (only for non-generic types as GetTypeByMetadataName requires backticks for generics)
        if (genericStartIndex == -1 && !normalizedTypeName.Contains('<'))
        {
            var type = compilation.GetTypeByMetadataName(normalizedTypeName);
            if (type != null)
                return type;
        }

        var symbols = GetSymbolsByName(compilation, simpleName);
        return FindBestExceptionMatch(symbols, normalizedTypeName, typeNameWithoutGenerics);
    }

    private static List<ITypeSymbol> GetSymbolsByName(Compilation compilation, string simpleName)
    {
        // Try lookup by name (e.g. "ArgumentNullException" instead of "System.ArgumentNullException")
        var symbols = compilation.GetSymbolsWithName(simpleName, SymbolFilter.Type)
                                 .OfType<ITypeSymbol>()
                                 .Where(t => !t.IsImplicitlyDeclared)
                                 .ToList();

        if (symbols.Count != 0)
            return symbols;

        var stack = new Stack<INamespaceSymbol>();
        stack.Push(compilation.GlobalNamespace);
        while (stack.Count > 0)
        {
            var ns = stack.Pop();
            foreach (var member in ns.GetMembers())
            {
                switch (member)
                {
                    case INamespaceSymbol nested:
                        stack.Push(nested);
                        break;
                    case ITypeSymbol type when type.Name == simpleName:
                        symbols.Add(type);
                        break;
                }
            }
        }

        return symbols;
    }

    private static ITypeSymbol? FindBestExceptionMatch(List<ITypeSymbol> symbols, string normalizedTypeName, string typeNameWithoutGenerics)
    {
        ITypeSymbol? genericFallback = null;

        foreach (var t in symbols)
        {
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
            var syntax = syntaxReference.GetSyntax(cancellationToken);
            var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

            var nodes = GetDescendantNodesOfInterest(syntax, isPrimaryCtor);
            var exceptions = IdentifyThrownExceptions(nodes, semanticModel, options, exceptionCache, cancellationToken);

            thrownTypes.UnionWith(exceptions);
        }

        return thrownTypes;
    }

    private static HashSet<ITypeSymbol> GetThrownTypes(SymbolAnalysisContext context, ISymbol symbol, bool isPrimaryCtor, CommentSenseOptions options)
    {
        return GetThrownTypes(context.Compilation, symbol, isPrimaryCtor, options, context.CancellationToken);
    }

    private static IEnumerable<SyntaxNode> GetDescendantNodesOfInterest(SyntaxNode root, bool isPrimaryCtor)
    {
        return root.DescendantNodes(n =>
        {
            // Ensure we don't block the root node (ClassDeclaration is BaseTypeDeclaration)
            if (n == root)
                return true;

            if (n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax)
                return false;

            if (isPrimaryCtor && n is MemberDeclarationSyntax)
                return n is FieldDeclarationSyntax;

            return true;
        }).Where(n =>
        {
            if (!isPrimaryCtor)
                return true;

            if (n is MemberDeclarationSyntax and not FieldDeclarationSyntax)
                return false;

            return true;
        });
    }

    private static IEnumerable<ITypeSymbol> IdentifyThrownExceptions(IEnumerable<SyntaxNode> nodes, SemanticModel semanticModel, CommentSenseOptions options, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        var exceptionType = semanticModel.Compilation.GetTypeByMetadataName("System.Exception");
        if (exceptionType == null)
            yield break;

        foreach (var node in nodes)
        {
            var exceptions = GetExceptionsFromNode(node, semanticModel, options, exceptionType, exceptionCache, token);
            foreach (var exception in exceptions)
            {
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
            ObjectCreationExpressionSyntax objectCreation => GetExceptionsFromObjectCreation(objectCreation, semanticModel, options, exceptionCache, token),
            ImplicitObjectCreationExpressionSyntax implicitObjectCreation => GetExceptionsFromImplicitObjectCreation(implicitObjectCreation, semanticModel, options, exceptionCache, token),
            ConstructorInitializerSyntax ci when options.ScanCalledMethodsForExceptions =>
                GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(ci, token).Symbol, semanticModel.Compilation, exceptionCache),
            MemberAccessExpressionSyntax ma when options.ScanCalledMethodsForExceptions => GetExceptionsFromMemberAccess(ma, semanticModel, exceptionCache, token),
            MemberBindingExpressionSyntax mb when options.ScanCalledMethodsForExceptions => GetExceptionsFromMemberBinding(mb, semanticModel, exceptionCache, token),
            IdentifierNameSyntax id when options.ScanCalledMethodsForExceptions => GetExceptionsFromIdentifier(id, semanticModel, exceptionCache, token),
            ElementAccessExpressionSyntax elementAccess when options.ScanCalledMethodsForExceptions =>
                GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(elementAccess, token).Symbol, semanticModel.Compilation, exceptionCache),
            ElementBindingExpressionSyntax eb when options.ScanCalledMethodsForExceptions =>
                GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(eb, token).Symbol, semanticModel.Compilation, exceptionCache),
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

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromObjectCreation(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel, CommentSenseOptions options, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        return options.ScanCalledMethodsForExceptions
            ? GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(objectCreation, token).Symbol, semanticModel.Compilation, exceptionCache)
            : [];
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromImplicitObjectCreation(ImplicitObjectCreationExpressionSyntax implicitObjectCreation, SemanticModel semanticModel, CommentSenseOptions options, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        return options.ScanCalledMethodsForExceptions
            ? GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(implicitObjectCreation, token).Symbol, semanticModel.Compilation, exceptionCache)
            : [];
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromMemberAccess(MemberAccessExpressionSyntax ma, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        // Only process if it's NOT the expression of an invocation (that's handled by InvocationExpressionSyntax)
        return ma.Parent is InvocationExpressionSyntax parentInvocation && parentInvocation.Expression == ma
            ? []
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(ma, token).Symbol, semanticModel.Compilation, exceptionCache);
    }

    private static IEnumerable<ITypeSymbol?> GetExceptionsFromMemberBinding(MemberBindingExpressionSyntax mb, SemanticModel semanticModel, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        // Only process if it's NOT the expression of an invocation (that's handled by InvocationExpressionSyntax)
        return mb.Parent is InvocationExpressionSyntax parentInvocationMb && parentInvocationMb.Expression == mb
            ? []
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(mb, token).Symbol, semanticModel.Compilation, exceptionCache);
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
            : GetExceptionsFromSymbol(semanticModel.GetSymbolInfo(id, token).Symbol, semanticModel.Compilation, exceptionCache);
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromInvocation(InvocationExpressionSyntax invocation, SemanticModel semanticModel, ITypeSymbol exceptionType, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> exceptionCache, CancellationToken token)
    {
        var symbol = semanticModel.GetSymbolInfo(invocation, token).Symbol;

        var guardException = GetExceptionTypeFromGuardClause(invocation, symbol, exceptionType);
        var exceptions = GetExceptionsFromSymbol(symbol, semanticModel.Compilation, exceptionCache);

        bool guardExceptionFound = false;
        foreach (var exception in exceptions)
        {
            if (guardException != null && SymbolEqualityComparer.Default.Equals(exception, guardException))
                guardExceptionFound = true;

            yield return exception;
        }

        if (guardException != null && !guardExceptionFound)
            yield return guardException;
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromSymbol(ISymbol? symbol, Compilation compilation, ConcurrentDictionary<ISymbol, IEnumerable<ITypeSymbol>> cache)
    {
        if (symbol is not (IMethodSymbol or IPropertySymbol or IEventSymbol))
            return [];

        return cache.GetOrAdd(symbol, s => [.. GetExceptionsFromSymbolInternal(s, compilation)]);
    }

    private static IEnumerable<ITypeSymbol> GetExceptionsFromSymbolInternal(ISymbol symbol, Compilation compilation)
    {
        if (symbol is IMethodSymbol { MethodKind: MethodKind.DelegateInvoke } delegateMethod)
        {
            symbol = delegateMethod.ContainingType;
        }

        foreach (var cref in DocumentationXmlExtensions.GetExceptionCrefs(symbol.GetDocumentationCommentXml()))
        {
            if (ResolveExceptionType(cref, compilation) is { } resolved)
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

    private static void AddInheritedExceptions(XElement xml, ISymbol symbol, Compilation compilation, HashSet<ITypeSymbol> result, HashSet<ISymbol> visited, CancellationToken cancellationToken)
    {
        foreach (var inheritDoc in DocumentationXmlExtensions.GetTargetElements(xml, DocumentationTags.InheritDoc))
        {
            var cref = inheritDoc.Attribute(DocumentationAttributes.Cref)?.Value;
            if (cref != null && !string.IsNullOrEmpty(cref))
            {
                var resolved = ResolveSymbolFromCref(cref, compilation);
                if (resolved != null)
                {
                    result.UnionWith(GetDocumentedExceptionsCached(resolved, compilation, visited, cancellationToken));
                }
            }
            else
            {
                foreach (var baseMember in GetDefaultInheritDocTargets(symbol))
                {
                    result.UnionWith(GetDocumentedExceptionsCached(baseMember, compilation, visited, cancellationToken));
                }
            }
        }
    }

    private static ISymbol? ResolveSymbolFromCref(string cref, Compilation compilation)
    {
        cref = cref.Trim();

        var resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, compilation);
        if (resolved != null)
            return resolved;

        if (cref.Length >= 2 && cref[1] == ':')
            return null;

        string[] prefixes = ["T:", "M:", "P:"];
        foreach (var prefix in prefixes)
        {
            resolved = DocumentationCommentId.GetFirstSymbolForDeclarationId(prefix + cref, compilation);
            if (resolved != null)
                return resolved;
        }

        return null;
    }

    private static IImmutableSet<ITypeSymbol> GetDocumentedExceptionsCached(ISymbol symbol, Compilation compilation, HashSet<ISymbol> visited, CancellationToken cancellationToken)
    {
        var cache = DocumentedExceptionCache.GetValue(compilation, _ => new ConcurrentDictionary<ISymbol, IImmutableSet<ITypeSymbol>>(SymbolEqualityComparer.Default));

        if (cache.TryGetValue(symbol, out var cached))
            return cached;

        if (!visited.Add(symbol))
            return ImmutableHashSet<ITypeSymbol>.Empty;

        try
        {
            var result = new HashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            var xmlString = symbol.GetDocumentationCommentXml(expandIncludes: true, cancellationToken: cancellationToken);
            if (DocumentationXmlExtensions.TryParseDocumentation(xmlString, out var element))
            {
                // Current member's exceptions
                foreach (var cref in DocumentationXmlExtensions.GetExceptionCrefs(element))
                {
                    if (ResolveExceptionType(cref, compilation) is { } resolved)
                        result.Add(resolved);
                }

                // Inherited exceptions if it has inheritdoc
                AddInheritedExceptions(element, symbol, compilation, result, visited, cancellationToken);
            }

            var finalResult = result.ToImmutableHashSet<ITypeSymbol>(SymbolEqualityComparer.Default);
            cache.TryAdd(symbol, finalResult);
            return finalResult;
        }
        finally
        {
            visited.Remove(symbol);
        }
    }

    private static IEnumerable<ISymbol> GetDefaultInheritDocTargets(ISymbol symbol)
    {
        return symbol switch
        {
            IMethodSymbol method => GetMethodInheritDocTargets(method),
            IPropertySymbol property => GetPropertyInheritDocTargets(property),
            INamedTypeSymbol type => GetTypeInheritDocTargets(type),
            _ => []
        };
    }

    private static IEnumerable<ISymbol> GetMethodInheritDocTargets(IMethodSymbol method)
    {
        if (method.OverriddenMethod != null)
            yield return method.OverriddenMethod;

        foreach (var target in GetInterfaceImplementations(method, method.Name, method.ExplicitInterfaceImplementations))
            yield return target;
    }

    private static IEnumerable<ISymbol> GetPropertyInheritDocTargets(IPropertySymbol property)
    {
        if (property.OverriddenProperty != null)
            yield return property.OverriddenProperty;

        foreach (var target in GetInterfaceImplementations(property, property.Name, property.ExplicitInterfaceImplementations))
            yield return target;
    }

    private static IEnumerable<ISymbol> GetTypeInheritDocTargets(INamedTypeSymbol type)
    {
        if (type.BaseType != null)
            yield return type.BaseType;

        foreach (var iface in type.Interfaces)
        {
            yield return iface;
        }
    }

    private static IEnumerable<TSymbol> GetInterfaceImplementations<TSymbol>(TSymbol symbol, string name, ImmutableArray<TSymbol> explicitImpls)
        where TSymbol : class, ISymbol
    {
        foreach (var ifaceMember in explicitImpls)
        {
            yield return ifaceMember;
        }

        if (symbol.ContainingType is not { } containingType)
            yield break;

        foreach (var iface in containingType.AllInterfaces)
        {
            foreach (var ifaceMember in iface.GetMembers(name).OfType<TSymbol>())
            {
                if (SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(ifaceMember), symbol) &&
                    !explicitImpls.Any(i => SymbolEqualityComparer.Default.Equals(i, ifaceMember)))
                {
                    yield return ifaceMember;
                }
            }
        }
    }

    private static ITypeSymbol? GetCaughtExceptionType(ThrowStatementSyntax throwStatement, SemanticModel semanticModel, ITypeSymbol? exceptionType, CancellationToken cancellationToken)
    {
        var catchClause = throwStatement.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause?.Declaration is null)
            return exceptionType;

        return semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
    }
}
