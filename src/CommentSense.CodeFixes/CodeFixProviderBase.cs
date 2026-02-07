using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace CommentSense.CodeFixes;

/// <summary>
/// Provides a base class for code fix providers in CommentSense.
/// </summary>
public abstract class CodeFixProviderBase : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    /// <summary>
    /// Finds the <see cref="XmlTextSyntax"/> at the specified <paramref name="span"/>, with robustness for <paramref name="span"/> shifts.
    /// </summary>
    /// <param name="root">The syntax <paramref name="root"/> to search in.</param>
    /// <param name="span">The text <paramref name="span"/> to find.</param>
    /// <returns>The found <see cref="XmlTextSyntax"/>, or <see langword="null"/> if not found.</returns>
    protected static XmlTextSyntax? FindXmlText(SyntaxNode root, TextSpan span)
    {
        if (root.FullSpan.Contains(span))
        {
            var node = root.FindNode(span, findInsideTrivia: true, getInnermostNodeForTie: true);
            var xmlText = node.FirstAncestorOrSelf<XmlTextSyntax>();
            if (xmlText != null) return xmlText;
        }

        if (span.Start >= root.FullSpan.Start && span.Start < root.FullSpan.End)
        {
            var token = root.FindToken(span.Start, findInsideTrivia: true);
            var parent = token.Parent;
            if (parent != null)
            {
                return parent.FirstAncestorOrSelf<XmlTextSyntax>();
            }
        }

        return null;
    }

    /// <summary>
    /// Replaces a span of text within a <see cref="XmlTextSyntax"/> with a sequence of <see cref="XmlNodeSyntax"/> nodes.
    /// </summary>
    /// <param name="document">The <paramref name="document"/> to modify.</param>
    /// <param name="diagnosticSpan">The <paramref name="diagnosticSpan"/> of the diagnostic to fix.</param>
    /// <param name="createReplacementNodes">A delegate that creates the replacement nodes.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> used to cancel the operation.</param>
    /// <returns>The updated <paramref name="document"/>.</returns>
    protected static async Task<Document> ReplaceTextWithNodesAsync(Document document, TextSpan diagnosticSpan, Func<XmlTextSyntax, int, int, int, IEnumerable<XmlNodeSyntax>> createReplacementNodes, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var xmlText = FindXmlText(root, diagnosticSpan);
        if (xmlText == null) return document;

        var tokenToSplit = xmlText.TextTokens.FirstOrDefault(t => t.Span.Contains(diagnosticSpan));
        if (tokenToSplit.IsKind(SyntaxKind.None))
        {
            tokenToSplit = xmlText.TextTokens.FirstOrDefault(t => t.Span.IntersectsWith(diagnosticSpan));
        }

        if (tokenToSplit.IsKind(SyntaxKind.None)) return document;

        var tokenIndex = xmlText.TextTokens.IndexOf(tokenToSplit);
        var relativeStart = Math.Max(0, diagnosticSpan.Start - tokenToSplit.SpanStart);
        var relativeEnd = Math.Min(tokenToSplit.Span.Length, diagnosticSpan.End - tokenToSplit.SpanStart);

        var docTrivia = xmlText.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>();
        if (docTrivia == null) return document;

        var replacementNodes = createReplacementNodes(xmlText, tokenIndex, relativeStart, relativeEnd);

        var parent = xmlText.Parent;
        if (parent == null) return document;

        SyntaxNode? updatedParent = parent switch
        {
            XmlElementSyntax xmlElement => xmlElement.WithContent(xmlElement.Content.ReplaceRange(xmlText, replacementNodes)),
            DocumentationCommentTriviaSyntax docTriviaContent => docTriviaContent.WithContent(docTriviaContent.Content.ReplaceRange(xmlText, replacementNodes)),
            _ => null
        };

        if (updatedParent == null) return document;

        var newDocTrivia = parent == docTrivia
            ? (DocumentationCommentTriviaSyntax)updatedParent
            : docTrivia.ReplaceNode(parent, updatedParent);
        var oldTrivia = docTrivia.ParentTrivia;
        var token = oldTrivia.Token;

        if (token == default)
            token = root.FindToken(oldTrivia.SpanStart);

        if (token == default)
            return document;

        var newToken = token.ReplaceTrivia(oldTrivia, SyntaxFactory.Trivia(newDocTrivia));
        return document.WithSyntaxRoot(root.ReplaceToken(token, newToken));
    }

    /// <summary>
    /// Creates a list of replacement nodes for the specified <see cref="XmlTextSyntax"/> and relative split points.
    /// </summary>
    /// <param name="xmlText">The XML text to modify.</param>
    /// <param name="tokenIndex">The index of the token to split.</param>
    /// <param name="relativeStart">The start of the split relative to the token.</param>
    /// <param name="relativeEnd">The end of the split relative to the token.</param>
    /// <param name="replacementNode">The node to insert in the middle.</param>
    /// <returns>A list of replacement nodes.</returns>
    protected static List<XmlNodeSyntax> CreateReplacementNodes(XmlTextSyntax xmlText, int tokenIndex, int relativeStart, int relativeEnd, XmlNodeSyntax replacementNode)
    {
        var tokenToSplit = xmlText.TextTokens[tokenIndex];

        var prefixText = tokenToSplit.Text.Substring(0, relativeStart);
        var suffixText = tokenToSplit.Text.Substring(relativeEnd);

        var replacementNodes = new List<XmlNodeSyntax>();

        var prefixTokens = new List<SyntaxToken>(xmlText.TextTokens.Take(tokenIndex));
        var finalReplacementNode = replacementNode;

        if (!string.IsNullOrEmpty(prefixText))
        {
            prefixTokens.Add(SyntaxFactory.XmlTextLiteral(tokenToSplit.LeadingTrivia, prefixText, prefixText, SyntaxTriviaList.Empty));
        }
        else
        {
            finalReplacementNode = finalReplacementNode.WithLeadingTrivia(tokenToSplit.LeadingTrivia);
        }

        if (prefixTokens.Count > 0)
            replacementNodes.Add(SyntaxFactory.XmlText(SyntaxFactory.TokenList(prefixTokens)));

        var suffixTokens = new List<SyntaxToken>();
        if (!string.IsNullOrEmpty(suffixText))
        {
            suffixTokens.Add(SyntaxFactory.XmlTextLiteral(SyntaxTriviaList.Empty, suffixText, suffixText, tokenToSplit.TrailingTrivia));
        }
        else
        {
            finalReplacementNode = finalReplacementNode.WithTrailingTrivia(tokenToSplit.TrailingTrivia);
        }

        replacementNodes.Add(finalReplacementNode);

        suffixTokens.AddRange(xmlText.TextTokens.Skip(tokenIndex + 1));

        if (suffixTokens.Count > 0)
            replacementNodes.Add(SyntaxFactory.XmlText(SyntaxFactory.TokenList(suffixTokens)));

        return replacementNodes;
    }
}
