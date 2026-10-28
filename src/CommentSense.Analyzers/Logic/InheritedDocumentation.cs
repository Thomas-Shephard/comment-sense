using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace CommentSense.Analyzers.Logic;

internal static class InheritedDocumentation
{
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, Result>> Cache = new();

    internal readonly record struct Result(ImmutableArray<XElement> Elements, bool HasUnknownInclude, bool HasPathFilter);

    public static Result Resolve(Compilation compilation, ISymbol symbol, CancellationToken cancellationToken)
    {
        var cache = Cache.GetValue(compilation, _ => new ConcurrentDictionary<ISymbol, Result>(SymbolEqualityComparer.Default));
        return cache.GetOrAdd(symbol, s => Resolve(compilation, s, new HashSet<ISymbol>(SymbolEqualityComparer.Default),
            new Dictionary<ISymbol, Result>(SymbolEqualityComparer.Default), cancellationToken, out _));
    }

    public static bool HasTag(Compilation compilation, ISymbol symbol, DocumentationComment documentation, string tag, string? name, CancellationToken cancellationToken)
    {
        if (documentation.GetElements(DocumentationTags.Include).Any())
            return true;

        if (!documentation.GetElements(DocumentationTags.InheritDoc).Any())
            return false;

        var inherited = Resolve(compilation, symbol, cancellationToken);
        return !inherited.HasPathFilter || inherited.HasUnknownInclude || inherited.Elements.Any(element =>
            element.Name.LocalName == tag && (name is null || (string?)element.Attribute(DocumentationAttributes.Name) == name));
    }

    private static Result Resolve(Compilation compilation, ISymbol symbol, HashSet<ISymbol> visited, Dictionary<ISymbol, Result> resolved, CancellationToken cancellationToken, out bool hasCycle)
    {
        cancellationToken.ThrowIfCancellationRequested();
        hasCycle = false;
        if (symbol is IMethodSymbol { PartialDefinitionPart: { } definition })
            symbol = definition;
        if (resolved.TryGetValue(symbol, out var cached))
            return cached;
        if (!visited.Add(symbol))
        {
            hasCycle = true;
            return new Result([], false, false);
        }

        try
        {
            if (!DocumentationXmlExtensions.TryParseDocumentation(symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken), out var documentation))
                return new Result([], false, false);

            var elements = ImmutableArray.CreateBuilder<XElement>();
            var seenElements = new HashSet<XElement>(XNode.EqualityComparer);
            var unknown = false;
            var hasPathFilter = false;
            foreach (var element in DocumentationXmlExtensions.GetTargetElements(documentation))
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (element.Name.LocalName != DocumentationTags.InheritDoc)
                {
                    elements.Add(element);
                    seenElements.Add(element);
                    unknown |= element.Name.LocalName == DocumentationTags.Include;
                    continue;
                }

                var targets = GetTargets(compilation, symbol, element);
                var path = (string?)element.Attribute(DocumentationAttributes.Path);
                hasPathFilter |= !string.IsNullOrWhiteSpace(path);
                if (targets.IsEmpty && symbol.DeclaringSyntaxReferences.IsEmpty && string.IsNullOrWhiteSpace(path))
                    unknown = true;

                foreach (var target in targets)
                {
                    var inherited = Resolve(compilation, target, visited, resolved, cancellationToken, out var targetHasCycle);
                    hasCycle |= targetHasCycle;
                    hasPathFilter |= inherited.HasPathFilter;
                    var selected = SelectElements(inherited.Elements, path);
                    elements.AddRange(selected.Where(seenElements.Add));
                    unknown |= selected.Any(selectedElement => selectedElement.Name.LocalName == DocumentationTags.Include) ||
                        inherited.HasUnknownInclude && string.IsNullOrWhiteSpace(path);
                }
            }

            var result = new Result(elements.ToImmutable(), unknown, hasPathFilter);
            // Cyclic results depend on the active ancestors and cannot be reused on another branch.
            if (!hasCycle)
                resolved[symbol] = result;
            return result;
        }
        finally
        {
            visited.Remove(symbol);
        }
    }

    private static ImmutableArray<ISymbol> GetTargets(Compilation compilation, ISymbol symbol, XElement inheritDoc)
    {
        var cref = (string?)inheritDoc.Attribute(DocumentationAttributes.Cref);
        if (cref is null)
            return InheritDocAnalyzer.GetImplicitTargetsForInheritDoc(symbol);

        var target = DocumentationCommentId.GetFirstSymbolForDeclarationId(cref, compilation);
        return target is null ? [] : [target];
    }

    private static ImmutableArray<XElement> SelectElements(ImmutableArray<XElement> elements, string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return elements;

        try
        {
            using var text = new StringReader(string.Concat(elements.Select(element => element.ToString(SaveOptions.DisableFormatting))));
            using var reader = XmlReader.Create(text, new XmlReaderSettings { ConformanceLevel = ConformanceLevel.Fragment, DtdProcessing = DtdProcessing.Prohibit });
            var navigator = new XPathDocument(reader).CreateNavigator();
            var selected = navigator.Select(path);
            var result = ImmutableArray.CreateBuilder<XElement>();
            while (selected.MoveNext())
            {
                if (selected.Current is { NodeType: XPathNodeType.Element } node)
                    result.Add(XElement.Parse(node.OuterXml));
                else if (selected.Current is { NodeType: XPathNodeType.Root } root)
                    result.AddRange(XElement.Parse($"<root>{root.InnerXml}</root>").Elements());
            }
            return result.ToImmutable();
        }
        catch (XPathException)
        {
            return [];
        }
    }
}
