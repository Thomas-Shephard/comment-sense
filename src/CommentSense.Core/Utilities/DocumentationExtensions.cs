using Microsoft.CodeAnalysis;

namespace CommentSense.Core.Utilities;

internal static class DocumentationExtensions
{
    public static bool HasValidDocumentation(this ISymbol? symbol)
    {
        if (symbol is null)
            return false;

        return DocumentationXmlExtensions.HasValidDocumentation(symbol.GetDocumentationCommentXml());
    }
}
