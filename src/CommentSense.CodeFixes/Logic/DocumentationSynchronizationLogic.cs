using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

internal static class DocumentationSynchronizationLogic
{
    public readonly struct MatchResult(XmlNodeSyntax node, string tagName, string oldName, string newName, double similarity, ISymbol symbol)
    {
        public XmlNodeSyntax Node { get; } = node;
        public string TagName { get; } = tagName;
        public string OldName { get; } = oldName;
        public string NewName { get; } = newName;
        public double Similarity { get; } = similarity;
        public ISymbol Symbol { get; } = symbol;
    }

    public static Task<MatchResult?> FindMatchAsync(
        SyntaxNode root,
        SemanticModel semanticModel,
        Diagnostic diagnostic,
        CommentSenseOptions options,
        Dictionary<ISymbol, (string? XML, System.Xml.Linq.XElement? Element)>? docCache,
        CancellationToken cancellationToken)
    {
        if (options.RenameSimilarityThreshold <= 0)
            return Task.FromResult<MatchResult?>(null);

        var node = root.FindNode(diagnostic.Location.SourceSpan, findInsideTrivia: true);
        var symbol = node.GetAssociatedSymbol(semanticModel);
        if (symbol == null)
            return Task.FromResult<MatchResult?>(null);

        var xElement = GetDocumentationElement(symbol, docCache, cancellationToken);
        if (xElement == null)
            return Task.FromResult<MatchResult?>(null);

        var isTypeParam = diagnostic.Id is CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId or CommentSenseDiagnosticIds.StrayTypeParameterDocumentationId;
        var tagName = isTypeParam ? DocumentationTags.TypeParam : DocumentationTags.Param;

        var result = diagnostic.Id is CommentSenseDiagnosticIds.MissingParameterDocumentationId or CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId
            ? FindMatchForMissing(node, symbol, tagName, diagnostic, options.RenameSimilarityThreshold)
            : FindMatchForStray(root, symbol, xElement, tagName, diagnostic, isTypeParam, options.RenameSimilarityThreshold);

        return Task.FromResult(result);
    }

    public static System.Xml.Linq.XElement? GetDocumentationElement(
        ISymbol symbol,
        Dictionary<ISymbol, (string? XML, System.Xml.Linq.XElement? Element)>? docCache,
        CancellationToken cancellationToken)
    {
        if (docCache != null && docCache.TryGetValue(symbol, out var cached))
            return cached.Element;

        var xml = symbol.GetDocumentationCommentXml(cancellationToken: cancellationToken);
        if (!DocumentationXmlExtensions.TryParseDocumentation(xml, out var xElement))
            return null;

        docCache?[symbol] = (xml, xElement);
        return xElement;
    }

    public static MatchResult? FindMatchForMissing(
        SyntaxNode node,
        ISymbol symbol,
        string tagName,
        Diagnostic diagnostic,
        double threshold)
    {
        if (!diagnostic.Properties.TryGetValue(DocumentationAttributes.NameProperty, out var missingName) || missingName == null)
            return null;

        var memberDecl = node.GetMemberDeclaration();
        if (memberDecl == null)
            return null;

        var docTrivia = memberDecl.GetLeadingTrivia().Select(t => t.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().LastOrDefault();
        if (docTrivia == null)
            return null;

        var expectedNames = symbol.GetExpectedMemberNames(tagName).ToImmutableHashSet();
        var strayTags = docTrivia.Content.OfType<XmlElementSyntax>()
            .Where(e => e.StartTag.Name.LocalName.ValueText == tagName && !expectedNames.Contains(e.GetNameAttribute() ?? ""))
            .Cast<XmlNodeSyntax>()
            .Concat(docTrivia.Content.OfType<XmlEmptyElementSyntax>()
                .Where(e => e.Name.LocalName.ValueText == tagName && !expectedNames.Contains(e.GetNameAttribute() ?? "")))
            .ToList();

        return FindBestMatch(strayTags, missingName, threshold, symbol);
    }

    public static MatchResult? FindMatchForStray(
        SyntaxNode root,
        ISymbol symbol,
        System.Xml.Linq.XElement xElement,
        string tagName,
        Diagnostic diagnostic,
        bool isTypeParam,
        double threshold)
    {
        var xmlNode = FindXmlNode(root, diagnostic.Location.SourceSpan);
        if (xmlNode is null)
            return null;

        var strayName = xmlNode.GetNameAttribute();
        if (strayName is null || string.IsNullOrEmpty(strayName))
            return null;

        var documentedNames = DocumentationXmlExtensions.GetNames(xElement, tagName, topLevelOnly: true).ToImmutableHashSet();
        IEnumerable<ISymbol> missingSymbols = isTypeParam
            ? symbol.GetTypeParameters().Where(p => !documentedNames.Contains(p.Name))
            : symbol.GetParameters().Where(p => !documentedNames.Contains(p.Name));

        var (bestName, bestSimilarity) = missingSymbols
                          .Select(s => (s.Name, Similarity: strayName.CalculateSimilarity(s.Name)))
                          .Where(x => x.Similarity >= threshold)
                          .OrderByDescending(x => x.Similarity)
                          .FirstOrDefault();

        if (bestName != null)
            return new MatchResult(xmlNode, tagName, strayName, bestName, bestSimilarity, symbol);

        return null;
    }

    public static MatchResult? FindBestMatch(IEnumerable<XmlNodeSyntax> nodes, string targetName, double threshold, ISymbol symbol)
    {
        var candidates = new List<(XmlNodeSyntax Node, string Name, double Similarity)>();
        foreach (var node in nodes)
        {
            var name = node.GetNameAttribute();
            if (name is not null)
                candidates.Add((node, name, name.CalculateSimilarity(targetName)));
        }

        var (bestNode, bestNodeName, bestSimilarity) = candidates
            .Where(x => x.Similarity >= threshold)
            .OrderByDescending(x => x.Similarity)
            .FirstOrDefault();

        if (bestNode != null && bestNodeName != null)
            return new MatchResult(bestNode, bestNode.GetTagName(), bestNodeName, targetName, bestSimilarity, symbol);

        return null;
    }

    public static XmlNodeSyntax? FindXmlNode(SyntaxNode root, Microsoft.CodeAnalysis.Text.TextSpan span)
    {
        var node = root.FindNode(span, findInsideTrivia: true);
        return node as XmlNodeSyntax ?? node.AncestorsAndSelf().OfType<XmlNodeSyntax>().FirstOrDefault();
    }

    public static XmlElementStartTagSyntax RenameAttribute(XmlElementStartTagSyntax startTag, string newName)
    {
        return startTag.WithAttributes(RenameAttribute(startTag.Attributes, newName));
    }

    public static SyntaxList<XmlAttributeSyntax> RenameAttribute(SyntaxList<XmlAttributeSyntax> attributes, string newName)
    {
        for (int i = 0; i < attributes.Count; i++)
        {
            if (attributes[i] is XmlNameAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } nameAttr)
            {
                var newNameAttr = nameAttr.WithIdentifier(SyntaxFactory.IdentifierName(newName));
                return attributes.Replace(attributes[i], newNameAttr);
            }

            if (attributes[i] is XmlTextAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Name } textAttr)
            {
                if (textAttr.TextTokens.Count == 0)
                    continue;

                var firstToken = textAttr.TextTokens[0];
                var lastToken = textAttr.TextTokens[textAttr.TextTokens.Count - 1];
                var newToken = SyntaxFactory.XmlTextLiteral(newName)
                    .WithLeadingTrivia(firstToken.LeadingTrivia)
                    .WithTrailingTrivia(lastToken.TrailingTrivia);
                var newTextAttr = textAttr.WithTextTokens(SyntaxFactory.TokenList(newToken));
                return attributes.Replace(attributes[i], newTextAttr);
            }
        }

        return attributes;
    }
}
