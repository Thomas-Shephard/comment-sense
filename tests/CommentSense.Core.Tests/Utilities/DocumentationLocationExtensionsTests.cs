using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CommentSense.Core.Utilities;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationLocationExtensionsTests
{
    [Test]
    public void GetDocumentationCommentTriviaWithTriviaReturnsTrivia()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Not.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaWithRegularCommentReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("// regular comment\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaWithDirectiveReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("#if true\npublic class C {}\n#endif");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnDetachedNodeReturnsNull()
    {
        var detached = SyntaxFactory.IdentifierName("x");
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(detached), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnCompilationUnitReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("using System;");
        var node = tree.GetRoot();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnMemberWithoutTriviaReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetPrimaryLocationSingleLocationReturnsLocation()
    {
        var location = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var locations = ImmutableArray.Create(location);
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(location));
    }

    [Test]
    public void GetPrimaryLocationEmptyListReturnsNone()
    {
        var locations = ImmutableArray<Location>.Empty;
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(Location.None));
    }

    [Test]
    public void GetPrimaryLocationDefaultArrayReturnsNone()
    {
        var locations = default(ImmutableArray<Location>);
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(Location.None));
    }

    [Test]
    public void GetDocumentationCommentTriviaWithMixedTriviaReturnsTrivia()
    {
        const string source = "#if true\n/// <summary>S</summary>\npublic class C {}\n#endif";
        var tree = CSharpSyntaxTree.ParseText(source);
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Not.Null);
    }
}
