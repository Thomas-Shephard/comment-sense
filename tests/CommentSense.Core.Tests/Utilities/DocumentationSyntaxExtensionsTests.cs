using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CommentSense.Core.Utilities;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationSyntaxExtensionsTests
{
    [Test]
    public void GetTagNameXmlElementReturnsCorrectName()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary>S</summary>\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("summary"));
    }

    [Test]
    public void GetTagNameXmlEmptyElementReturnsCorrectName()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <inheritdoc />\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("inheritdoc"));
    }

    [Test]
    public void IsPureWhitespaceOrPrefixOnlyPrefixAndWhitespaceReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("///\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void GetParentContentExistingElementReturnsContent()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary>S</summary>\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Not.Null);
            Assert.That(content, Is.Not.Empty);
        }
    }

    [Test]
    public void GetNameAttributeExistingAttributeReturnsValue()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <param name=\"x\">P</param>\npublic void M(int x) {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("x"));
    }

    [Test]
    public void GetNameAttributeTextAttributeReturnsValue()
    {
        var attr = SyntaxFactory.XmlTextAttribute(
            SyntaxFactory.XmlName("name"),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("value")),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("mytag"), SyntaxFactory.List<XmlAttributeSyntax>([attr]));
        Assert.That(node.GetNameAttribute(), Is.EqualTo("value"));
    }

    [Test]
    public void GetNameAttributeMismatchedNameReturnsNull()
    {
        var attr = SyntaxFactory.XmlTextAttribute(
            SyntaxFactory.XmlName("wrong"),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("val")),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("mytag"), SyntaxFactory.List<XmlAttributeSyntax>([attr]));
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void GetNameAttributeCrefAttributeReturnsNull()
    {
        var cref = SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        var attr = SyntaxFactory.XmlCrefAttribute(cref);
        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("mytag"), SyntaxFactory.List<XmlAttributeSyntax>([attr]));
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void GetMemberDeclarationNullNodeReturnsNull()
    {
        Assert.That(((SyntaxNode?)null).GetMemberDeclaration(), Is.Null);
    }

    [Test]
    public void GetMemberDeclarationElementNodeReturnsDeclaration()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary>S</summary>\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        var decl = node.GetMemberDeclaration();
        Assert.That(decl, Is.InstanceOf<ClassDeclarationSyntax>());
    }

    [Test]
    public void GetMemberDeclarationNormalNodeReturnsSelfOrParent()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        var decl = node.GetMemberDeclaration();
        Assert.That(decl, Is.InstanceOf<ClassDeclarationSyntax>());
    }

    [Test]
    public void GetMemberDeclarationDetachedNodeReturnsNull()
    {
        var detached = SyntaxFactory.XmlElement(SyntaxFactory.XmlName("summary"), SyntaxFactory.List<XmlNodeSyntax>());
        Assert.That(detached.GetMemberDeclaration(), Is.Null);
    }

    [Test]
    public void GetMemberDeclarationTriviaWithoutParentReturnsNull()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia);
        var xml = SyntaxFactory.XmlElement(SyntaxFactory.XmlName("summary"), SyntaxFactory.List<XmlNodeSyntax>());
        _ = trivia.WithContent(SyntaxFactory.List<XmlNodeSyntax>([xml]));

        Assert.That(xml.GetMemberDeclaration(), Is.Null);
    }

    [Test]
    public void GetMemberDeclarationDetachedTriviaReturnsNull()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia);
        Assert.That(trivia.GetMemberDeclaration(), Is.Null);
    }

    [Test]
    public void GetMemberDeclarationUsingDirectiveReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary/>\nusing System;");
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetMemberDeclaration(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveDocumentationElementReturnsWhitespaceNode()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary/> \n/// <remarks/>\npublic class C {}");
        var nodes = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).ToList();
        var summary = nodes.OfType<XmlEmptyElementSyntax>().First(e => e.Name.LocalName.ValueText == "summary");
        var remarks = nodes.OfType<XmlEmptyElementSyntax>().First(e => e.Name.LocalName.ValueText == "remarks");

        var trivia = summary.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>() ?? throw new InvalidOperationException();
        var content = trivia.Content;

        var result1 = DocumentationSyntaxExtensions.GetAssociatedWhitespaceToRemove(summary, content);
        Assert.That(result1, Is.Not.Null);
        Assert.That(result1.ToString(), Does.Contain("\n"));

        var result2 = DocumentationSyntaxExtensions.GetAssociatedWhitespaceToRemove(remarks, content);
        Assert.That(result2, Is.Not.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveUnsupportedParentReturnsNull()
    {
        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("summary"));
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }
}
