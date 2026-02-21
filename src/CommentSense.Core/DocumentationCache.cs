using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Xml.Linq;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;

namespace CommentSense.Core;

internal static class DocumentationCache
{
    private static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ISymbol, XElement?>> Cache = new();

    public static XElement? GetOrParseDocumentation(Compilation compilation, ISymbol symbol)
    {
        var compilationCache = Cache.GetValue(compilation, _ => new ConcurrentDictionary<ISymbol, XElement?>(SymbolEqualityComparer.Default));

        return compilationCache.GetOrAdd(symbol, s =>
        {
            var xml = s.GetDocumentationCommentXml();
            if (DocumentationXmlExtensions.TryParseDocumentation(xml, out var element))
                return element;

            return null;
        });
    }
}
