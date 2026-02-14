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
using Microsoft.CodeAnalysis.Text;

namespace CommentSense.CodeFixes.Logic;

/// <summary>
/// Provides a code fix that resolves invalid exception types in &lt;exception&gt; tags.
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExceptionResolutionCodeFixProvider)), Shared]
public class ExceptionResolutionCodeFixProvider : CodeFixProviderBase
{
    /// <inheritdoc />
    public sealed override ImmutableArray<string> FixableDiagnosticIds => [CommentSenseDiagnosticIds.InvalidExceptionTypeId, CommentSenseDiagnosticIds.UnresolvedCrefId];

    /// <inheritdoc />
    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
        if (root == null) return;

        foreach (var diagnostic in context.Diagnostics)
        {
            if (diagnostic.Properties.TryGetValue(DocumentationAttributes.CrefProperty, out var suggestedCref) && suggestedCref != null)
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: string.Format(CultureInfo.InvariantCulture, Resources.ExceptionResolutionTitle, suggestedCref),
                        createChangedDocument: c => FixCrefAsync(context.Document, diagnostic.Location.SourceSpan, suggestedCref, c),
                        equivalenceKey: nameof(ExceptionResolutionCodeFixProvider)),
                    diagnostic);
            }
        }
    }

    private static async Task<Document> FixCrefAsync(Document document, TextSpan diagnosticSpan, string suggestedCref, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        if (root == null) return document;

        var node = root.FindNode(diagnosticSpan, findInsideTrivia: true);
        var crefAttr = node.FirstAncestorOrSelf<XmlCrefAttributeSyntax>();
        if (crefAttr == null) return document;

        var newCref = DocumentationSyntaxExtensions.ParseCref(suggestedCref);
        var newCrefAttr = crefAttr.WithCref(newCref);

        var newRoot = root.ReplaceNode(crefAttr, newCrefAttr);
        return document.WithSyntaxRoot(newRoot);
    }
}
