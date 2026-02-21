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
        if (DocumentationXmlExtensions.HasInheritDoc(xml) || DocumentationXmlExtensions.HasAutoValidTag(xml))
            return;

        var filteredThrownTypes = new List<ITypeSymbol>();
        foreach (var thrownType in thrownTypes)
        {
            if (IsIgnored(thrownType, options))
                continue;

            bool alreadyDocumented = documentedTypes.Contains(thrownType);
            if (!alreadyDocumented)
            {
                foreach (var documentedType in documentedTypes)
                {
                    if (!thrownType.InheritsFromOrEquals(documentedType))
                        continue;

                    alreadyDocumented = true;
                    break;
                }
            }

            if (!alreadyDocumented)
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

        var ns = type.ContainingNamespace?.ToDisplayString() ?? string.Empty;
        if (options.IgnoreSystemExceptions && IsInNamespace(ns, "System"))
            return true;

        if (options.IgnoredExceptionNamespaces.Any(targetNs => IsInNamespace(ns, targetNs)))
            return true;

        if (options.IgnoredExceptions.Contains(type.ToDisplayString(FullNameFormat)))
            return true;

        if (type is INamedTypeSymbol { IsGenericType: true } named && options.IgnoredExceptions.Contains(named.OriginalDefinition.ToDisplayString(FullNameFormat)))
            return true;

        return false;
    }

    internal static bool IsInNamespace(string ns, string targetNamespace)
    {
        if (ns.Equals(targetNamespace, StringComparison.OrdinalIgnoreCase))
            return true;

        return ns.StartsWith(targetNamespace + ".", StringComparison.OrdinalIgnoreCase);
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

        var normalizedCref = DocumentationSyntaxExtensions.NormalizeCref(crefText);
        var crefSpan = normalizedCref.AsSpan();
        int lastDotIndex = crefSpan.LastIndexOf('.');
        var lastPart = lastDotIndex == -1 ? crefSpan : crefSpan.Slice(lastDotIndex + 1);
        var simpleCrefName = RemoveGenerics(lastPart);

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
            if ((simpleCrefName.Length > 2 && (single.Name.IndexOf(simpleCrefName, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 simpleCrefName.IndexOf(single.Name, StringComparison.OrdinalIgnoreCase) >= 0)) ||
                single.Name.CalculateSimilarity(simpleCrefName) >= options.RenameSimilarityThreshold)
            {
                return single.ToCrefString();
            }
        }

        // 3. Fuzzy match by name similarity
        ITypeSymbol? bestMatch = null;
        double bestSimilarity = -1.0;

        foreach (var t in thrownTypes)
        {
            double similarity = t.Name.CalculateSimilarity(simpleCrefName);
            if (similarity < options.RenameSimilarityThreshold || similarity <= bestSimilarity)
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

            if (n is AnonymousFunctionExpressionSyntax or LocalFunctionStatementSyntax or BaseTypeDeclarationSyntax)
                return false;

            if (isPrimaryCtor && IsExcludedPrimaryConstructorMember(n))
                return false;

            return true;
        });
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

        foreach (var exception in GetExceptionsFromSymbol(symbol, semanticModel.Compilation, exceptionCache))
        {
            if (guardException != null && SymbolEqualityComparer.Default.Equals(exception, guardException))
                guardException = null;

            yield return exception;
        }

        if (guardException != null)
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

    private static ITypeSymbol? GetCaughtExceptionType(ThrowStatementSyntax throwStatement, SemanticModel semanticModel, ITypeSymbol? exceptionType, CancellationToken cancellationToken)
    {
        var catchClause = throwStatement.Ancestors().OfType<CatchClauseSyntax>().FirstOrDefault();
        if (catchClause?.Declaration is null)
            return exceptionType;

        return semanticModel.GetTypeInfo(catchClause.Declaration.Type, cancellationToken).Type;
    }
}
