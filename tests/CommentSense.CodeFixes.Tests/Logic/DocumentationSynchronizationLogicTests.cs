using System.Collections.Immutable;
using CommentSense.CodeFixes.Logic;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using Moq;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests.Logic;

public class DocumentationSynchronizationLogicTests
{
    [Test]
    public void RenameAttributeHandlesXmlTextAttributeSyntax()
    {
        var name = SyntaxFactory.XmlName(SyntaxFactory.Identifier("name"));
        var textTokens = SyntaxFactory.TokenList(
            SyntaxFactory.XmlTextLiteral(
                SyntaxFactory.TriviaList(),
                "oldName",
                "oldName",
                SyntaxFactory.TriviaList()
            )
        );

        var attr = SyntaxFactory.XmlTextAttribute(
            name,
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            textTokens,
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken)
        );

        var attributes = SyntaxFactory.List<XmlAttributeSyntax>([attr]);

        var result = DocumentationSynchronizationLogic.RenameAttribute(attributes, "newName");

        var newAttr = result[0] as XmlTextAttributeSyntax;
        Assert.That(newAttr, Is.Not.Null);
        Assert.That(newAttr.TextTokens.ToString(), Is.EqualTo("newName"));
    }

    [Test]
    public void RenameAttributeHandlesXmlTextAttributeSyntaxWithEmptyTokens()
    {
        var name = SyntaxFactory.XmlName(SyntaxFactory.Identifier("name"));
        var textTokens = SyntaxFactory.TokenList();

        var attr = SyntaxFactory.XmlTextAttribute(
            name,
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            textTokens,
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken)
        );

        var attributes = SyntaxFactory.List<XmlAttributeSyntax>([attr]);

        var result = DocumentationSynchronizationLogic.RenameAttribute(attributes, "newName");

        var newAttr = result[0] as XmlTextAttributeSyntax;
        Assert.That(newAttr, Is.Not.Null);
        Assert.That(newAttr.TextTokens, Is.Empty);
    }

    [Test]
    public async Task FindMatchAsyncReturnsNullWhenThresholdIsZero()
    {
        var options = CommentSenseOptions.Default with { RenameSimilarityThreshold = 0 };
        var root = SyntaxFactory.CompilationUnit();
        var semanticModel = new Mock<SemanticModel>();
        var diagnostic = Diagnostic.Create("CSENSE001", "Category", "Message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1);

        var result = await DocumentationSynchronizationLogic.FindMatchAsync(
            root,
            semanticModel.Object,
            diagnostic,
            options,
            null,
            CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindMatchAsyncReturnsNullWhenSymbolNotFound()
    {
        var options = CommentSenseOptions.Default with { RenameSimilarityThreshold = 0.5 };
        var tree = CSharpSyntaxTree.ParseText("");
        var root = await tree.GetRootAsync();

        var compilation = CSharpCompilation.Create("Test", syntaxTrees: [tree]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, new TextSpan(0, 0)));

        var result = await DocumentationSynchronizationLogic.FindMatchAsync(
            root,
            semanticModel,
            diagnostic,
            options,
            null,
            CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public async Task FindMatchAsyncReturnsNullWhenDocumentationIsMissing()
    {
        var options = CommentSenseOptions.Default with { RenameSimilarityThreshold = 0.5 };

        var tree = CSharpSyntaxTree.ParseText("class C { void M() {} }");
        var root = await tree.GetRootAsync();
        var methodDecl = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();
        var span = methodDecl.Span;

        var compilation = CSharpCompilation.Create("Test", syntaxTrees: [tree]);
        var semanticModel = compilation.GetSemanticModel(tree);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, span));

        var result = await DocumentationSynchronizationLogic.FindMatchAsync(
            root,
            semanticModel,
            diagnostic,
            options,
            null,
            CancellationToken.None);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForMissingReturnsNullWhenNamePropertyMissing()
    {
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None);

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            SyntaxFactory.CompilationUnit(),
            new Mock<ISymbol>().Object,
            "param",
            diagnostic,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForMissingReturnsNullWhenNotMemberDeclaration()
    {
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "missingParam");
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None,
            properties);

        var node = SyntaxFactory.CompilationUnit();

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            node,
            new Mock<ISymbol>().Object,
            "param",
            diagnostic,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForMissingReturnsNullWhenNoDocTrivia()
    {
        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "missingParam");
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None,
            properties);

        var node = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "M");

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            node,
            new Mock<ISymbol>().Object,
            "param",
            diagnostic,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForMissingIncludesXmlEmptyElementSyntax()
    {
        var nameAttr = SyntaxFactory.XmlNameAttribute(
            SyntaxFactory.XmlName("name"),
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.IdentifierName("stray"),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

        var element = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param"))
            .AddAttributes(nameAttr);

        var docTrivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([element]));

        var methodDecl = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "M")
            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Trivia(docTrivia)));

        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "missingParam");
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None,
            properties);

        var paramSymbol = new Mock<IParameterSymbol>();
        paramSymbol.Setup(p => p.Name).Returns("missingParam");

        var methodSymbol = new Mock<IMethodSymbol>();
        methodSymbol.Setup(m => m.Parameters).Returns([paramSymbol.Object]);

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            methodDecl,
            methodSymbol.Object,
            "param",
            diagnostic,
            0.0);

        Assert.That(result, Is.Not.Null);
        Assert.That(result.GetValueOrDefault().OldName, Is.EqualTo("stray"));
    }

    [Test]
    public void FindMatchForStrayReturnsNullWhenXmlNodeNotFound()
    {
        var root = SyntaxFactory.CompilationUnit();
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE003", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None);

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            new Mock<ISymbol>().Object,
            new System.Xml.Linq.XElement("member"),
            "param",
            diagnostic,
            false,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForStrayReturnsNullWhenNameAttributeMissing()
    {
        var code = """
            /// <summary>Summary</summary>
            /// <param>Content</param>
            class C { void M() { } }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var xmlNode = root.DescendantNodes(descendIntoTrivia: true)
                          .OfType<XmlElementSyntax>()
                          .FirstOrDefault(x => x.StartTag.Name.LocalName.ValueText == "param");
        Assert.That(xmlNode, Is.Not.Null);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE003", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, xmlNode.Span));

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            new Mock<ISymbol>().Object,
            new System.Xml.Linq.XElement("member"),
            "param",
            diagnostic,
            false,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForStrayReturnsNullWhenNameAttributeIsEmpty()
    {
        var code = """
            /// <summary>Summary</summary>
            /// <param name="">Content</param>
            class C { void M() { } }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var xmlNode = root.DescendantNodes(descendIntoTrivia: true)
                          .OfType<XmlElementSyntax>()
                          .FirstOrDefault(x => x.StartTag.Name.LocalName.ValueText == "param");
        Assert.That(xmlNode, Is.Not.Null);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE003", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, xmlNode.Span));

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            new Mock<ISymbol>().Object,
            new System.Xml.Linq.XElement("member"),
            "param",
            diagnostic,
            false,
            0.5);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void FindMatchForMissingIgnoredTags()
    {
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("summary")),
            SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName("summary")));

        var docTrivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([element]));

        var methodDecl = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "M")
            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Trivia(docTrivia)));

        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "missingParam");
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None,
            properties);

        var paramSymbol = new Mock<IParameterSymbol>();
        paramSymbol.Setup(p => p.Name).Returns("missingParam");
        var methodSymbol = new Mock<IMethodSymbol>();
        methodSymbol.Setup(m => m.Parameters).Returns([paramSymbol.Object]);

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            methodDecl,
            methodSymbol.Object,
            "param",
            diagnostic,
            0.0);

        Assert.That(result, Is.Null);
    }

    [Test]
    public void RenameAttributeIgnoresAttributesWithWrongName()
    {
        var name = SyntaxFactory.XmlName(SyntaxFactory.Identifier("other"));
        var textTokens = SyntaxFactory.TokenList(
            SyntaxFactory.XmlTextLiteral(
                SyntaxFactory.TriviaList(),
                "oldName",
                "oldName",
                SyntaxFactory.TriviaList()
            )
        );

        var attr = SyntaxFactory.XmlTextAttribute(
            name,
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            textTokens,
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken)
        );

        var attributes = SyntaxFactory.List<XmlAttributeSyntax>([attr]);

        var result = DocumentationSynchronizationLogic.RenameAttribute(attributes, "newName");

        var newAttr = result[0] as XmlTextAttributeSyntax;
        Assert.That(newAttr, Is.Not.Null);
        Assert.That(newAttr.TextTokens.ToString(), Is.EqualTo("oldName"));
    }

    [Test]
    public void RenameAttributeXmlElementStartTagSyntaxDirectCall()
    {
        var startTag = SyntaxFactory.XmlElementStartTag(
            SyntaxFactory.XmlName("param"),
            SyntaxFactory.List<XmlAttributeSyntax>([
                SyntaxFactory.XmlNameAttribute(
                    SyntaxFactory.XmlName("name"),
                    SyntaxFactory.Token(SyntaxKind.EqualsToken),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
                    SyntaxFactory.IdentifierName("oldName"),
                    SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken))
            ])
        );

        var result = DocumentationSynchronizationLogic.RenameAttribute(startTag, "newName");

        var newAttr = result.Attributes[0] as XmlNameAttributeSyntax;
        Assert.That(newAttr, Is.Not.Null);
        Assert.That(newAttr.Identifier.Identifier.ValueText, Is.EqualTo("newName"));
    }

    [Test]
    public void FindMatchForStrayFindsMatch()
    {
        var code = """
            /// <summary>Summary</summary>
            /// <param name="p11">Content</param>
            class C { void M(int p1) { } }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var xmlNode = root.DescendantNodes(descendIntoTrivia: true)
                          .OfType<XmlElementSyntax>()
                          .FirstOrDefault(x => x.StartTag.Name.LocalName.ValueText == "param");
        Assert.That(xmlNode, Is.Not.Null);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE003", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, xmlNode.Span));

        var methodSymbol = new Mock<IMethodSymbol>();
        var paramSymbol = new Mock<IParameterSymbol>();
        paramSymbol.Setup(p => p.Name).Returns("p1");
        methodSymbol.Setup(m => m.Parameters).Returns([paramSymbol.Object]);
        methodSymbol.Setup(m => m.TypeParameters).Returns([]);

        var xElement = new System.Xml.Linq.XElement("member",
            new System.Xml.Linq.XElement("summary", "Summary"),
            new System.Xml.Linq.XElement("param", new System.Xml.Linq.XAttribute("name", "p11"), "Content"));

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            methodSymbol.Object,
            xElement,
            "param",
            diagnostic,
            isTypeParam: false,
            threshold: 0.4);

        Assert.That(result, Is.Not.Null);
        var match = result.GetValueOrDefault();
        Assert.That(match.NewName, Is.EqualTo("p1"));
    }
    [Test]
    public void FindMatchForMissingHandlesFiltering()
    {
        var p1Attr = SyntaxFactory.XmlNameAttribute(
             SyntaxFactory.XmlName("name"),
             SyntaxFactory.Token(SyntaxKind.EqualsToken),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
             SyntaxFactory.IdentifierName("p1"),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
        var p1Tag = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param")).AddAttributes(p1Attr);

        var seeAttr = SyntaxFactory.XmlNameAttribute(
             SyntaxFactory.XmlName("cref"),
             SyntaxFactory.Token(SyntaxKind.EqualsToken),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
             SyntaxFactory.IdentifierName("something"),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
        var seeTag = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("see")).AddAttributes(seeAttr);

        var p11Attr = SyntaxFactory.XmlNameAttribute(
             SyntaxFactory.XmlName("name"),
             SyntaxFactory.Token(SyntaxKind.EqualsToken),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
             SyntaxFactory.IdentifierName("p11"),
             SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
        var p11Tag = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param")).AddAttributes(p11Attr);

        var docTrivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([p1Tag, seeTag, p11Tag]));

        var methodDecl = SyntaxFactory.MethodDeclaration(
                SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "M")
            .WithLeadingTrivia(SyntaxFactory.TriviaList(SyntaxFactory.Trivia(docTrivia)));

        var properties = ImmutableDictionary<string, string?>.Empty.Add("Name", "p1");
        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE002", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.None,
            properties);

        var methodSymbol = new Mock<IMethodSymbol>();

        var paramSymbol = new Mock<IParameterSymbol>();
        paramSymbol.Setup(p => p.Name).Returns("p1");
        methodSymbol.Setup(m => m.Parameters).Returns([paramSymbol.Object]);

        var result = DocumentationSynchronizationLogic.FindMatchForMissing(
            methodDecl,
            methodSymbol.Object,
            "param",
            diagnostic,
            0.4);

        Assert.That(result, Is.Not.Null);
        var match = result.GetValueOrDefault();
        Assert.That(match.OldName, Is.EqualTo("p11"));
    }

    [Test]
    public void RenameAttributeHandlesAttributesOtherThanName()
    {
        var otherAttr = SyntaxFactory.XmlTextAttribute(
            SyntaxFactory.XmlName("other"),
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("value")),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

        var nameAttr = SyntaxFactory.XmlNameAttribute(
            SyntaxFactory.XmlName("name"),
            SyntaxFactory.Token(SyntaxKind.EqualsToken),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.IdentifierName("oldName"),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));

        var attributes = SyntaxFactory.List<XmlAttributeSyntax>([otherAttr, nameAttr]);

        var result = DocumentationSynchronizationLogic.RenameAttribute(attributes, "newName");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(((XmlTextAttributeSyntax)result[0]).Name.LocalName.ValueText, Is.EqualTo("other"));
        }

        var renamed = result[1] as XmlNameAttributeSyntax;
        Assert.That(renamed, Is.Not.Null);
        Assert.That(renamed.Identifier.Identifier.ValueText, Is.EqualTo("newName"));
    }

    [Test]
    public void FindMatchForStrayFindsMatchTypeParam()
    {
        var code = """
            /// <summary>Summary</summary>
            /// <typeparam name="TT">Content</typeparam>
            class C<T> { }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var xmlNode = root.DescendantNodes(descendIntoTrivia: true)
                          .OfType<XmlElementSyntax>()
                          .FirstOrDefault(x => x.StartTag.Name.LocalName.ValueText == "typeparam");
        Assert.That(xmlNode, Is.Not.Null);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE005", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, xmlNode.Span));

        var typeSymbol = new Mock<INamedTypeSymbol>();
        var typeParamSymbol = new Mock<ITypeParameterSymbol>();
        typeParamSymbol.Setup(p => p.Name).Returns("T");

        typeSymbol.Setup(t => t.TypeParameters).Returns([typeParamSymbol.Object]);

        var xElement = new System.Xml.Linq.XElement("member",
            new System.Xml.Linq.XElement("summary", "Summary"),
            new System.Xml.Linq.XElement("typeparam", new System.Xml.Linq.XAttribute("name", "TT"), "Content"));

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            typeSymbol.Object,
            xElement,
            "typeparam",
            diagnostic,
            isTypeParam: true,
            threshold: 0.4);

        Assert.That(result, Is.Not.Null);
        var match = result.GetValueOrDefault();
        Assert.That(match.NewName, Is.EqualTo("T"));
    }

    [Test]
    public void FindMatchForStrayRespectsThreshold()
    {
        var code = """
            /// <summary>Summary</summary>
            /// <param name="veryDifferentName">Content</param>
            class C { void M(int p1) { } }
            """;
        var tree = CSharpSyntaxTree.ParseText(code);
        var root = tree.GetRoot();

        var xmlNode = root.DescendantNodes(descendIntoTrivia: true)
                          .OfType<XmlElementSyntax>()
                          .FirstOrDefault(x => x.StartTag.Name.LocalName.ValueText == "param");
        Assert.That(xmlNode, Is.Not.Null);

        var diagnostic = Diagnostic.Create(
            new DiagnosticDescriptor("CSENSE003", "Title", "Message", "Category", DiagnosticSeverity.Warning, true),
            Location.Create(tree, xmlNode.Span));

        var methodSymbol = new Mock<IMethodSymbol>();
        var paramSymbol = new Mock<IParameterSymbol>();
        paramSymbol.Setup(p => p.Name).Returns("p1");
        methodSymbol.Setup(m => m.Parameters).Returns([paramSymbol.Object]);
        methodSymbol.Setup(m => m.TypeParameters).Returns([]);

        var xElement = new System.Xml.Linq.XElement("member",
            new System.Xml.Linq.XElement("summary", "Summary"),
            new System.Xml.Linq.XElement("param", new System.Xml.Linq.XAttribute("name", "veryDifferentName"), "Content"));

        var result = DocumentationSynchronizationLogic.FindMatchForStray(
            root,
            methodSymbol.Object,
            xElement,
            "param",
            diagnostic,
            isTypeParam: false,
            threshold: 0.9); // High threshold

        Assert.That(result, Is.Null);
    }
}
