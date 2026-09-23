using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.Core.Utilities;

internal static class DocumentationLocationExtensions
{
    public static Location GetPrimaryLocation(this ImmutableArray<Location> locations)
    {
        if (locations.IsDefaultOrEmpty)
            return Location.None;

        return locations[0];
    }

    public static DocumentationCommentTriviaSyntax? GetDocumentationCommentTrivia(SyntaxNode? syntax)
    {
        SyntaxNode? current = syntax;
        while (current != null)
        {
            if (current.HasStructuredTrivia)
            {
                var docTrivia = FindDocumentationTrivia(current.GetLeadingTrivia());
                if (docTrivia != null)
                    return docTrivia;
            }

            if (current is MemberDeclarationSyntax or CompilationUnitSyntax)
                return null;

            current = current.Parent;
        }

        return null;
    }

    private static DocumentationCommentTriviaSyntax? FindDocumentationTrivia(SyntaxTriviaList triviaList)
    {
        for (int i = triviaList.Count - 1; i >= 0; i--)
        {
            var trivia = triviaList[i];
            if (!trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && !trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia))
                continue;

            if (trivia.GetStructure() is DocumentationCommentTriviaSyntax docTrivia)
                return docTrivia;
        }

        return null;
    }
}
