using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class DocumentationExtensions
{
    public static bool HasValidDocumentation(this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        if (DocumentationComment.FromSymbol(symbol) is { } documentation && !documentation.IsMalformedFor(symbol))
            return documentation.HasValidDocumentation();

        return DocumentationXmlExtensions.HasValidDocumentation(symbol.GetDocumentationCommentXml());
    }
}
