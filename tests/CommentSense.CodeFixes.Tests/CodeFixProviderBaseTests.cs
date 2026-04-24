using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using System.Collections.Immutable;

namespace CommentSense.CodeFixes.Tests;

public class CodeFixProviderBaseTests
{
    private sealed class TestCodeFixProvider : CodeFixProviderBase
    {
        public override ImmutableArray<string> FixableDiagnosticIds => [];
        public override Task RegisterCodeFixesAsync(Microsoft.CodeAnalysis.CodeFixes.CodeFixContext context) => Task.CompletedTask;

        public static XmlTextSyntax? PublicFindXmlText(SyntaxNode root, TextSpan span) => FindXmlText(root, span);
        public static Task<Document> PublicReplaceTextWithNodesAsync(Document document, TextSpan span, Func<XmlTextSyntax, int, int, int, IEnumerable<XmlNodeSyntax>> createReplacementNodes, CancellationToken cancellationToken)
            => ReplaceTextWithNodesAsync(document, span, createReplacementNodes, cancellationToken);
        public static SyntaxNode? TryCreateUpdatedParentForTest(SyntaxNode parent, XmlTextSyntax xmlText, IEnumerable<XmlNodeSyntax> replacementNodes)
            => TryCreateUpdatedParent(parent, xmlText, replacementNodes);
    }

    private sealed class TestFixAllProvider() : CodeFixProviderBase.FixAllProviderBase("Test")
    {
        public int InvocationCount { get; private set; }

        public Task<Document> ApplyAsync(Document document, ImmutableArray<Diagnostic> diagnostics)
            => ApplyDocumentFixesAsync(document, diagnostics, CancellationToken.None);

        internal override Task<Document> FixDocumentInternalAsync(Document document, ImmutableArray<Diagnostic> diagnostics, CancellationToken cancellationToken)
        {
            InvocationCount++;
            return Task.FromResult(document);
        }
    }

    [Test]
    public void FindXmlTextFindsNodeDirectly()
    {
        const string source = """
            /// <summary>abc</summary>
            public class C {}
            """;
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var xmlText = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(x => x.ToString().Contains("abc"));
        var span = xmlText.Span;

        var result = TestCodeFixProvider.PublicFindXmlText(root, span);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(xmlText));
    }

    [Test]
    public void FindXmlTextFallbackToTokenWhenSpanOverlaps()
    {
        const string source = """
            /// <summary>abc</summary>
            public class C {}
            """;
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var xmlText = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(x => x.ToString().Contains("abc"));

        var startOfEndTag = xmlText.Span.End;
        var overlappingSpan = new TextSpan(startOfEndTag - 1, 2);

        var nodeFound = root.FindNode(overlappingSpan, findInsideTrivia: true, getInnermostNodeForTie: true);
        Assert.That(nodeFound.FirstAncestorOrSelf<XmlTextSyntax>(), Is.Null, "FindNode should not find XmlTextSyntax for overlapping span");

        var result = TestCodeFixProvider.PublicFindXmlText(root, overlappingSpan);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(xmlText));
    }

    [Test]
    public void FindXmlTextRecoversFromPartiallyOutOfBoundsSpan()
    {
        const string source = """
            /// <summary>abc</summary>
            public class C {}
            """;
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var xmlText = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(x => x.ToString().Contains("abc"));

        var partialSpan = new TextSpan(xmlText.Span.Start, root.FullSpan.End + 100);

        var result = TestCodeFixProvider.PublicFindXmlText(root, partialSpan);

        Assert.That(result, Is.Not.Null);
        Assert.That(result, Is.EqualTo(xmlText));
    }

    [Test]
    public void FindXmlTextReturnsNullWhenNoXmlTextAncestor()
    {
        const string source = "public class C {}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        // Find the 'public' keyword
        var token = root.FindToken(0);
        var span = token.Span;

        var result = TestCodeFixProvider.PublicFindXmlText(root, span);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindXmlTextReturnsNullWhenFarOutside()
    {
        const string source = """
            /// <summary>abc</summary>
            public class C {}
            """;
        var tree = CSharpSyntaxTree.ParseText(source);
        var root = tree.GetRoot();

        var farOutsideSpan = new TextSpan(root.FullSpan.End + 10, 1);

        var result = TestCodeFixProvider.PublicFindXmlText(root, farOutsideSpan);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task ReplaceTextWithNodesAsyncReturnsDocumentWhenXmlTextCannotBeFound()
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", "public class C {}");

        var result = await TestCodeFixProvider.PublicReplaceTextWithNodesAsync(
            document,
            new TextSpan(0, 1),
            (_, _, _, _) => throw new AssertionException("Replacement callback should not run."),
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(document));
    }

    [Test]
    public async Task ReplaceTextWithNodesAsyncReturnsDocumentWhenSpanNoLongerMatchesAnyXmlTextToken()
    {
        const string source = """
            /// <summary>abc</summary>
            public class C {}
            """;

        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", source);
        var root = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException();
        var xmlText = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(x => x.ToString().Contains("abc"));
        var token = xmlText.TextTokens.Single();
        var tokenWithTrivia = SyntaxFactory.XmlTextLiteral(
            SyntaxFactory.TriviaList(SyntaxFactory.Whitespace(" ")),
            token.Text,
            token.ValueText,
            token.TrailingTrivia);
        var updatedXmlText = xmlText.WithTextTokens(SyntaxFactory.TokenList(tokenWithTrivia));
        var updatedRoot = root.ReplaceNode(xmlText, updatedXmlText);
        document = document.WithSyntaxRoot(updatedRoot);

        var staleSpan = new TextSpan(updatedXmlText.FullSpan.Start, 1);

        var result = await TestCodeFixProvider.PublicReplaceTextWithNodesAsync(
            document,
            staleSpan,
            (_, _, _, _) => throw new AssertionException("Replacement callback should not run."),
            CancellationToken.None);

        Assert.That(result, Is.EqualTo(document));
    }

    [Test]
    public void TryCreateUpdatedParentReturnsNullForUnexpectedParentKind()
    {
        var xmlText = SyntaxFactory.XmlText("abc");
        var unexpectedParent = SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("summary"));

        var result = TestCodeFixProvider.TryCreateUpdatedParentForTest(
            unexpectedParent,
            xmlText,
            [SyntaxFactory.XmlEmptyElement("see")]);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void TryCreateUpdatedParentUpdatesXmlElement()
    {
        var originalElement = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("summary")),
            SyntaxFactory.List<XmlNodeSyntax>([SyntaxFactory.XmlText("abc")]),
            SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("summary")));
        var xmlText = originalElement.Content.OfType<XmlTextSyntax>().Single();

        var result = TestCodeFixProvider.TryCreateUpdatedParentForTest(
            originalElement,
            xmlText,
            [SyntaxFactory.XmlEmptyElement("see")]);

        Assert.That(result, Is.TypeOf<XmlElementSyntax>());
        var updatedElement = result as XmlElementSyntax ?? throw new InvalidOperationException();
        Assert.That(updatedElement.ToString(), Is.EqualTo("<summary><see/></summary>"));
    }

    [Test]
    public async Task ApplyDocumentFixesAsyncReturnsOriginalDocumentWhenDiagnosticsAreEmpty()
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", "public class C {}");
        var provider = new TestFixAllProvider();

        var result = await provider.ApplyAsync(document, []);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(document));
            Assert.That(provider.InvocationCount, Is.Zero);
        }
    }

    [Test]
    public async Task ApplyDocumentFixesAsyncInvokesFixWhenDiagnosticsExist()
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", "public class C {}");
        var tree = await document.GetSyntaxTreeAsync() ?? throw new InvalidOperationException();
        var provider = new TestFixAllProvider();
        var diagnostic = Diagnostic.Create(
            "ID",
            "Category",
            "Message",
            DiagnosticSeverity.Warning,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            warningLevel: 1,
            location: Location.Create(tree, new TextSpan(0, 1)));

        var result = await provider.ApplyAsync(document, [diagnostic]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.EqualTo(document));
            Assert.That(provider.InvocationCount, Is.EqualTo(1));
        }
    }
}
