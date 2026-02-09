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
}
