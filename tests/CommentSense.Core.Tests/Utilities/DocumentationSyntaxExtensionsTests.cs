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

    [Test]
    public void GetIndentationWithWhitespaceReturnsIndentation()
    {
        var tree = CSharpSyntaxTree.ParseText("    public class C {}");
        var member = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>().First();
        Assert.That(member.GetIndentation(), Is.EqualTo("    "));
    }

    [Test]
    public void GetIndentationNoWhitespaceReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}");
        var member = tree.GetRoot().DescendantNodes().OfType<MemberDeclarationSyntax>().First();
        Assert.That(member.GetIndentation(), Is.EqualTo(string.Empty));
    }

    [Test]
    public void GetNewLineWithWindowsLineEndingReturnsWindowsLineEnding()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}\r\n");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(node.GetNewLine(), Is.EqualTo("\r\n"));
    }

    [Test]
    public void GetNewLineWithUnixLineEndingReturnsUnixLineEnding()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}\n");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(node.GetNewLine(), Is.EqualTo("\n"));
    }

    [Test]
    public void GetNewLineNoLineEndingReturnsEnvironmentNewLine()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(node.GetNewLine(), Is.EqualTo(Environment.NewLine));
    }

    [Test]
    public void GetNewLineFromTriviaDetectsWindowsNewLine()
    {
        var eol = SyntaxFactory.EndOfLine("\r\n");
        var text = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(SyntaxTriviaList.Empty, "///", "///", SyntaxTriviaList.Create(eol))));
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List<XmlNodeSyntax>([text]));
        Assert.That(trivia.GetNewLine(), Is.EqualTo("\r\n"));
    }

    [Test]
    public void GetNewLineFromTriviaDetectsUnixNewLine()
    {
        var eol = SyntaxFactory.EndOfLine("\n");
        var text = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(SyntaxTriviaList.Empty, "///", "///", SyntaxTriviaList.Create(eol))));
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List<XmlNodeSyntax>([text]));
        Assert.That(trivia.GetNewLine(), Is.EqualTo("\n"));
    }

    [Test]
    public void GetNewLineFromTriviaDetectsLiteralWindowsNewLineInText()
    {
        var crlf = "\r\n";
        var text = "/// line1" + crlf + "/// line2";
        var token = SyntaxFactory.Token(SyntaxTriviaList.Empty, SyntaxKind.XmlTextLiteralToken, text, text, SyntaxTriviaList.Empty);
        var xmlText = SyntaxFactory.XmlText(SyntaxFactory.TokenList(token));
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List<XmlNodeSyntax>([xmlText]));
        Assert.That(trivia.GetNewLine(), Is.EqualTo("\r\n"));
    }

    [Test]
    public void GetNewLineFromTriviaDetectsLiteralUnixNewLineInText()
    {
        var text = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(SyntaxTriviaList.Empty, "/// line1\n/// line2", "/// line1\n/// line2", SyntaxTriviaList.Empty)));
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxFactory.List<XmlNodeSyntax>([text]));
        // Fallback to manual check if firstNewLine is empty.
        Assert.That(trivia.GetNewLine(), Is.EqualTo("\n"));
    }

    [Test]
    public void GetNewLineFromTriviaFallsBackToDocumentNewLine()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(SyntaxKind.SingleLineDocumentationCommentTrivia);
        Assert.That(trivia.GetNewLine(), Is.EqualTo(Environment.NewLine));
    }

    [Test]
    public void GetPrefixSingleLineDocumentationReturnsTripleSlash()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary/>\npublic class C {}");
        var trivia = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<DocumentationCommentTriviaSyntax>().First();
        Assert.That(trivia.GetPrefix(), Is.EqualTo("/// "));
    }

    [Test]
    public void GetPrefixMultiLineDocumentationReturnsStar()
    {
        var tree = CSharpSyntaxTree.ParseText("/** <summary/> */\npublic class C {}");
        var trivia = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<DocumentationCommentTriviaSyntax>().First();
        Assert.That(trivia.GetPrefix(), Is.EqualTo(" * "));
    }

    [Test]
    public void CreateXmlTextReturnsCorrectText()
    {
        var text = "test content";
        var node = DocumentationSyntaxExtensions.CreateXmlText(text);
        Assert.That(node.ToString(), Is.EqualTo(text));
    }

    [Test]
    public void CreateXmlElementNoNameReturnsElement()
    {
        var node = DocumentationSyntaxExtensions.CreateXmlElement("summary", content: "TODO");
        Assert.That(node.ToString(), Is.EqualTo("<summary>TODO</summary>"));
    }

    [Test]
    public void CreateXmlElementWithNameReturnsElementWithAttribute()
    {
        var node = DocumentationSyntaxExtensions.CreateXmlElement("param", name: "x", content: "TODO");
        Assert.That(node.ToString(), Is.EqualTo("<param name=\"x\">TODO</param>"));
    }
}
