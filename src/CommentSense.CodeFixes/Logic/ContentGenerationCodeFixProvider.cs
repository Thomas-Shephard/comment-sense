using System.Collections.Immutable;
using System.Composition;
using System.Globalization;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that generates missing XML documentation tags.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ContentGenerationCodeFixProvider)), Shared]
public class ContentGenerationCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
    [
        CommentSenseDiagnosticIds.MissingDocumentationId,
        CommentSenseDiagnosticIds.MissingParameterDocumentationId,
        CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId,
        CommentSenseDiagnosticIds.MissingReturnValueDocumentationId,
        CommentSenseDiagnosticIds.MissingValueDocumentationId,
        CommentSenseDiagnosticIds.MissingExceptionDocumentationId,
        CommentSenseDiagnosticIds.MissingInheritDocId
    ];

    /// <inheritdoc />
    public override FixAllProvider GetFixAllProvider() => new ContentGenerationFixAllProvider();

    private sealed class ContentGenerationFixAllProvider() : FixAllProviderBase(Resources.AddMissingTagsTitle)
    {
        internal override async Task<Document> FixDocumentInternalAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            if (root == null)
                return document;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var options = CommentSenseOptions.GetOptions(document.Project.AnalyzerOptions.AnalyzerConfigOptionsProvider, root.SyntaxTree);

            // Group diagnostics by member and capture symbol upfront
            var memberGroups = GetMemberGroups(root, semanticModel, diagnostics, cancellationToken);
            if (memberGroups.Count == 0)
                return document;

            var groupsByMember = memberGroups.ToDictionary(g => g.Member, g => g);

            var newRoot = root.ReplaceNodes(groupsByMember.Keys, (oldNode, newNode) =>
            {
                if (newNode is not MemberDeclarationSyntax member)
                    return newNode;

                var (_, diagList, symbol) = groupsByMember[oldNode];
                var updatedMember = member;

                foreach (var diag in diagList)
                {
                    updatedMember = ApplyDiagnosticToMember(updatedMember, diag, symbol, options);
                }

                return updatedMember;
            });

            return document.WithSyntaxRoot(newRoot);
        }

        private static MemberDeclarationSyntax ApplyDiagnosticToMember(MemberDeclarationSyntax member, Diagnostic diag, ISymbol? symbol, CommentSenseOptions options)
        {
            string? name = GetTargetName(diag);
            string? tagName = GetTagNameForDiagnostic(diag.Id);

            var leadingTrivia = member.GetLeadingTrivia();
            DocumentationCommentTriviaSyntax? docTrivia = null;
            SyntaxTrivia targetTrivia = default;

            foreach (var trivia in leadingTrivia)
            {
                if (trivia.GetStructure() is not DocumentationCommentTriviaSyntax d)
                    continue;

                docTrivia = d;
                targetTrivia = trivia;
                break;
            }

            if (docTrivia == null)
                return AddNewDocumentationToMember(member, diag.Id, tagName, name, Resources.DocumentationPlaceholder);

            var effectiveTagName = tagName ?? (diag.Id == CommentSenseDiagnosticIds.MissingInheritDocId ? DocumentationTags.InheritDoc : DocumentationTags.Summary);
            var newDocTrivia = InsertTagToTrivia(docTrivia, effectiveTagName, name, symbol, Resources.DocumentationPlaceholder, options);
            return member.WithLeadingTrivia(leadingTrivia.Replace(targetTrivia, SyntaxFactory.Trivia(newDocTrivia)));
        }

        private static string? GetTargetName(Diagnostic diag)
        {
            switch (diag.Id)
            {
                case CommentSenseDiagnosticIds.MissingParameterDocumentationId:
                case CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId:
                    {
                        diag.Properties.TryGetValue(DocumentationAttributes.NameProperty, out var name);
                        return name;
                    }
                case CommentSenseDiagnosticIds.MissingExceptionDocumentationId:
                    {
                        diag.Properties.TryGetValue(DocumentationAttributes.CrefProperty, out var name);
                        return name;
                    }
                default:
                    return null;
            }
        }

        private static string? GetTagNameForDiagnostic(string diagnosticId)
        {
            return diagnosticId switch
            {
                CommentSenseDiagnosticIds.MissingDocumentationId => null,
                CommentSenseDiagnosticIds.MissingParameterDocumentationId => DocumentationTags.Param,
                CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId => DocumentationTags.TypeParam,
                CommentSenseDiagnosticIds.MissingReturnValueDocumentationId => DocumentationTags.Returns,
                CommentSenseDiagnosticIds.MissingValueDocumentationId => DocumentationTags.Value,
                CommentSenseDiagnosticIds.MissingExceptionDocumentationId => DocumentationTags.Exception,
                _ => null
            };
        }

        private static MemberDeclarationSyntax AddNewDocumentationToMember(MemberDeclarationSyntax member, string diagnosticId, string? tagName, string? name, string placeholder)
        {
            var indentation = member.GetIndentation();
            var newLine = member.GetNewLine();

            var content = new List<XmlNodeSyntax>
            {
                // Initial prefix
                DocumentationSyntaxExtensions.CreateXmlText("/// ")
            };

            if (diagnosticId == CommentSenseDiagnosticIds.MissingInheritDocId)
            {
                content.Add(SyntaxFactory.XmlEmptyElement(
                    SyntaxFactory.Token(SyntaxKind.LessThanToken),
                    SyntaxFactory.XmlName(DocumentationTags.InheritDoc),
                    SyntaxFactory.List<XmlAttributeSyntax>(),
                    SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken).WithLeadingTrivia(SyntaxFactory.Whitespace(" "))));
            }
            else
            {
                content.Add(DocumentationSyntaxExtensions.CreateXmlElement(tagName ?? DocumentationTags.Summary, name, placeholder));
            }

            // Final newline
            content.Add(DocumentationSyntaxExtensions.CreateXmlText(newLine));

            var docTrivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List(content));
            var newTrivia = SyntaxFactory.Trivia(docTrivia);

            // [Whitespace][DocTrivia][Whitespace(indentation)]
            var newLeadingTrivia = member.GetLeadingTrivia().Add(newTrivia).Add(SyntaxFactory.Whitespace(indentation));
            return member.WithLeadingTrivia(newLeadingTrivia);
        }
    }

    internal static List<(MemberDeclarationSyntax Member, List<Diagnostic> Diagnostics, ISymbol? Symbol)> GetMemberGroups(
        SyntaxNode root, SemanticModel? semanticModel, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
    {
        var groups = diagnostics
            .Select(d => (Diagnostic: d, Member: root.FindNode(d.Location.SourceSpan).FirstAncestorOrSelf<MemberDeclarationSyntax>()))
            .Where(x => x.Member != null)
            .GroupBy(x => x.Member);

        var result = new List<(MemberDeclarationSyntax Member, List<Diagnostic> Diagnostics, ISymbol? Symbol)>();
        foreach (var g in groups)
        {
            var member = g.Key;
            if (member != null)
            {
                result.Add((member, g.Select(x => x.Diagnostic).ToList(), semanticModel?.GetDeclaredSymbol(member, cancellationToken)));
            }
        }

        return result;
    }

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var member = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            if (member == null)
                continue;

            string title;
            string? name;
            string? tagName;

            switch (diagnostic.Id)
            {
                case CommentSenseDiagnosticIds.MissingDocumentationId:
                    tagName = DocumentationTags.Summary;
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingTagTitle, tagName, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingParameterDocumentationId:
                    tagName = DocumentationTags.Param;
                    diagnostic.Properties.TryGetValue(DocumentationAttributes.NameProperty, out name);
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingNamedTagTitle, tagName, name ?? string.Empty, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId:
                    tagName = DocumentationTags.TypeParam;
                    diagnostic.Properties.TryGetValue(DocumentationAttributes.NameProperty, out name);
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingNamedTagTitle, tagName, name ?? string.Empty, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingReturnValueDocumentationId:
                    tagName = DocumentationTags.Returns;
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingTagTitle, tagName, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingValueDocumentationId:
                    tagName = DocumentationTags.Value;
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingTagTitle, tagName, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingExceptionDocumentationId:
                    tagName = DocumentationTags.Exception;
                    diagnostic.Properties.TryGetValue(DocumentationAttributes.CrefProperty, out name);
                    title = string.Format(CultureInfo.InvariantCulture, Resources.AddMissingCrefTagTitle, tagName, name ?? string.Empty, Resources.DocumentationPlaceholder);
                    break;
                case CommentSenseDiagnosticIds.MissingInheritDocId:
                    title = Resources.AddInheritDocTitle;
                    break;
                default:
                    continue;
            }

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => FixAsync(context.Document, diagnostic, c),
                    equivalenceKey: diagnostic.Id),
                diagnostic);
        }
    }

    private static async Task<Document> FixAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
    {
        var fixAllProvider = new ContentGenerationFixAllProvider();
        return await fixAllProvider.FixDocumentInternalAsync(document, [diagnostic], cancellationToken).ConfigureAwait(false);
    }

    internal static DocumentationCommentTriviaSyntax InsertTagToTrivia(DocumentationCommentTriviaSyntax docTrivia, string tagName, string? name, ISymbol? symbol, string placeholder = "", CommentSenseOptions? options = null)
    {
        var effectiveOptions = options ?? CommentSenseOptions.Default;
        var member = docTrivia.GetMemberDeclaration();
        var indentation = member?.GetIndentation() ?? string.Empty;

        var content = docTrivia.Content;
        var insertionIndex = FindInsertionIndex(content, tagName, name, symbol, effectiveOptions);

        var newLine = docTrivia.GetNewLine();
        var prefix = docTrivia.GetPrefix();

        var newNodes = new List<XmlNodeSyntax>();

        if (NeedsLeadingTrivia(docTrivia, content, insertionIndex, prefix, newLine))
            newNodes.Add(DocumentationSyntaxExtensions.CreateXmlText(newLine + indentation + prefix));

        newNodes.Add(CreateTagNode(tagName, name, placeholder));

        AddTrailingTrivia(newNodes, content, insertionIndex, newLine, indentation, prefix);

        var newContent = content.InsertRange(insertionIndex, newNodes);
        return docTrivia.WithContent(newContent);
    }

    private static bool NeedsLeadingTrivia(DocumentationCommentTriviaSyntax docTrivia, SyntaxList<XmlNodeSyntax> content, int insertionIndex, string prefix, string newLine)
    {
        if (insertionIndex <= 0)
            return false;

        if (IsImmediatelyAfterInitialPrefix(docTrivia, content, insertionIndex, prefix, newLine))
            return false;

        return content[insertionIndex - 1] is not XmlTextSyntax textNode || !textNode.ToString().Contains(newLine);
    }

    private static bool IsImmediatelyAfterInitialPrefix(DocumentationCommentTriviaSyntax docTrivia, SyntaxList<XmlNodeSyntax> content, int insertionIndex, string prefix, string newLine)
    {
        if (insertionIndex != 1 || content.Count == 0 || content[0] is not XmlTextSyntax initialText)
            return false;

        var textStr = initialText.ToString();
        if (textStr.Contains(newLine))
            return false;

        var trimmedText = textStr.TrimEnd();
        var trimmedPrefix = prefix.TrimEnd();

        return trimmedText.EndsWith(trimmedPrefix, StringComparison.Ordinal) ||
               trimmedText.EndsWith("/**", StringComparison.Ordinal) ||
               (docTrivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) && string.IsNullOrWhiteSpace(textStr));
    }

    private static XmlNodeSyntax CreateTagNode(string tagName, string? name, string placeholder)
    {
        if (tagName == DocumentationTags.InheritDoc)
        {
            return SyntaxFactory.XmlEmptyElement(
                SyntaxFactory.Token(SyntaxKind.LessThanToken),
                SyntaxFactory.XmlName(DocumentationTags.InheritDoc),
                SyntaxFactory.List<XmlAttributeSyntax>(),
                SyntaxFactory.Token(SyntaxKind.SlashGreaterThanToken).WithLeadingTrivia(SyntaxFactory.Whitespace(" ")));
        }

        return DocumentationSyntaxExtensions.CreateXmlElement(tagName, name, placeholder);
    }

    private static void AddTrailingTrivia(List<XmlNodeSyntax> newNodes, SyntaxList<XmlNodeSyntax> content, int insertionIndex, string newLine, string indentation, string prefix)
    {
        if (insertionIndex < content.Count && content[insertionIndex] is not XmlTextSyntax)
        {
            newNodes.Add(DocumentationSyntaxExtensions.CreateXmlText(newLine + indentation + prefix));
        }
        else if (insertionIndex == content.Count && (content.Count == 0 || !content.Last().ToString().Contains(newLine)))
        {
            newNodes.Add(DocumentationSyntaxExtensions.CreateXmlText(newLine));
        }
    }

    private static int FindInsertionIndex(SyntaxList<XmlNodeSyntax> content, string tagName, string? targetName, ISymbol? symbol, CommentSenseOptions options)
    {
        var order = GetTagOrder(tagName, options.TagOrder);
        var expectedNames = symbol?.GetExpectedMemberNames(tagName) ?? [];
        var targetExpectedIndex = targetName != null ? expectedNames.IndexOf(targetName) : -1;

        int bestIndex = -1;
        for (int i = 0; i < content.Count; i++)
        {
            var node = content[i];
            if (node is not (XmlElementSyntax or XmlEmptyElementSyntax))
                continue;

            if (ShouldInsertBefore(node, order, targetExpectedIndex, expectedNames, options.TagOrder))
                return i;

            bestIndex = i + 1;
        }

        if (bestIndex != -1)
            return bestIndex;

        if (content.Count <= 0 || content[0] is not XmlTextSyntax firstText)
            return content.Count;

        var text = firstText.ToString().TrimStart();
        if (text.StartsWith("///", StringComparison.Ordinal))
            return 1;
        if (text.StartsWith("/**", StringComparison.Ordinal))
            return 1;

        return content.Count;
    }

    private static int GetTagOrder(string tagName, IReadOnlyDictionary<string, int> tagOrder)
    {
        return tagOrder.TryGetValue(tagName, out var order) ? order : 100;
    }

    private static bool ShouldInsertBefore(XmlNodeSyntax node, int targetOrder, int targetExpectedIndex, ImmutableArray<string> expectedNames, IReadOnlyDictionary<string, int> tagOrder)
    {
        var currentTagName = node.GetTagName();
        var currentOrder = GetTagOrder(currentTagName, tagOrder);

        if (currentOrder > targetOrder)
            return true;

        if (currentOrder != targetOrder)
            return false;

        if (targetExpectedIndex == -1)
            return false;

        var currentName = node.GetNameAttribute();
        if (currentName == null)
            return true;

        var currentExpectedIndex = expectedNames.IndexOf(currentName);
        return currentExpectedIndex > targetExpectedIndex;
    }
}
