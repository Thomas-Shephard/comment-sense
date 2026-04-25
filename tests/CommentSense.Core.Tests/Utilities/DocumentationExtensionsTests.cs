using System.Xml.Linq;
using System.Collections.Immutable;
using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Moq;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationExtensionsTests
{
    [Test]
    public void HasValidDocumentationWithSummaryReturnsTrue()
    {
        const string source = """
            /// <summary>
            /// This is a summary for the class.
            /// </summary>
            public class TestClass {}
            """;
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithInheritDocReturnsTrue()
    {
        const string source = """
            public class C {
                /// <inheritdoc />
                public void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithNestedInheritDocReturnsTrue()
    {
        const string source = """
            public class C {
                /// <summary><inheritdoc /></summary>
                public void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithoutDocumentationReturnsFalse()
    {
        const string source = "public class TestClass {}";
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithParamReturnsTrue()
    {
        const string source = """
            public class TestClass {
                /// <param name="x">The param x.</param>
                public void M(int x) {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptySummaryReturnsTrue()
    {
        const string source = """
            /// <summary>
            /// </summary>
            public class TestClass {}
            """;
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationNullSymbolReturnsFalse()
    {
        Assert.That(((ISymbol?)null).HasValidDocumentation(), Is.False);
    }

    [Test]
    public void HasValidDocumentationNullStringReturnsFalse()
    {
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation((string?)null), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithRemarksReturnsTrue()
    {
        const string source = """
            /// <remarks>Some remarks</remarks>
            public class TestClass {}
            """;
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithReturnsReturnsTrue()
    {
        const string source = """
            public class C {
                /// <returns>A value</returns>
                public int M() => 0;
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithValueReturnsTrue()
    {
        const string source = """
            public class C {
                /// <value>The prop</value>
                public int P { get; set; }
            }
            """;
        var symbol = GetSymbolFromSource(source, "P");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithExceptionReturnsTrue()
    {
        const string source = """
            public class C {
                /// <exception cref="System.Exception">Thrown always</exception>
                public void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyParamReturnsTrue()
    {
        const string source = """
            public class C {
                /// <param name="x"></param>
                public void M(int x) {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyExceptionReturnsTrue()
    {
        const string source = """
            public class C {
                /// <exception cref="System.Exception"></exception>
                public void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationMalformedXmlReturnsFalse()
    {
        const string xml = "<invalid";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithIncludeTagReturnsTrue()
    {
        const string xml = """<member><include file='docs.xml' path='[@name="test"]'/></member>""";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithNestedElementsInSummaryReturnsTrue()
    {
        const string xml = """<member><summary><see cref="T:System.String"/></summary></member>""";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyExceptionXmlReturnsTrue()
    {
        const string xml = """<member><exception cref="T:System.Exception"/></member>""";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyTagsReturnsTrue()
    {
        const string xml = "<member><summary> </summary><remarks/></member>";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithTypeParamReturnsTrue()
    {
        const string xml = """<member><typeparam name="T">The type.</typeparam></member>""";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithPartialTypeDocumentationSplitAcrossDeclarationsReturnsTrue()
    {
        const string source = """
            /// <summary>This is a summary for the class.</summary>
            public partial class TestClass<T> {}

            /// <typeparam name="T">The type parameter.</typeparam>
            public partial class TestClass<T> {}
            """;
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void GetTypeParamNamesPreservesDeclarationOrderAcrossPartialDeclarations()
    {
        const string firstSource = """
            /// <typeparam name="T1">First type parameter.</typeparam>
            public partial class TestClass<T1, T2> {}
            """;
        const string secondSource = """
            /// <typeparam name="T2">Second type parameter.</typeparam>
            public partial class TestClass<T1, T2> {}
            """;

        var symbol = GetSymbolFromSources(
            ("z-last.cs", secondSource),
            ("a-first.cs", firstSource),
            "TestClass");

        var documentationComment = DocumentationComment.FromSymbol(symbol);
        Assert.That(documentationComment, Is.Not.Null);

        var expected = DocumentationXmlExtensions.GetTypeParamNames(symbol.GetDocumentationCommentXml()).ToList();
        var result = documentationComment.GetAttributeValues("typeparam", "name", topLevelOnly: true);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result, Is.EqualTo(expected));
        }
    }

    [Test]
    public void GetElementsPreservesPartialMethodDeclarationOrder()
    {
        const string source = """
            public partial class MyClass
            {
                /// <summary>Executes the operation.</summary>
                public partial void MyMethod(int p1);
            }

            public partial class MyClass
            {
                /// <param name="p1">The first parameter.</param>
                public partial void MyMethod(int p1) { }
            }
            """;

        var symbol = GetSymbolFromSource(source, "MyMethod");
        var documentationComment = DocumentationComment.FromSymbol(symbol);
        Assert.That(documentationComment, Is.Not.Null);

        var result = documentationComment.GetElements(recursive: false).Select(static element => element.GetTagName()).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(2));
            Assert.That(result[0], Is.EqualTo("summary"));
            Assert.That(result[1], Is.EqualTo("param"));
        }
    }

    [Test]
    public void GetMethodDeclarationOrderWithoutSyntaxReferencesReturnsImplementationOrder()
    {
        var method = new Mock<IMethodSymbol>();
        method.SetupGet(symbol => symbol.DeclaringSyntaxReferences).Returns(ImmutableArray<SyntaxReference>.Empty);

        Assert.That(InvokeGetMethodDeclarationOrder(method.Object), Is.EqualTo(1));
    }

    [Test]
    public void AddIfMissingIgnoresDuplicateSymbol()
    {
        var methodSymbol = (IMethodSymbol)RoslynTestUtils.GetSymbolFromSource("public class C { public void M() {} }", "M");
        var methods = new List<IMethodSymbol> { methodSymbol };

        InvokeAddIfMissing(methods, methodSymbol);
        Assert.That(methods, Has.Count.EqualTo(1));
    }

    [Test]
    public void OrderMethodSymbolsSingleMethodReturnsSameList()
    {
        var methodSymbol = (IMethodSymbol)RoslynTestUtils.GetSymbolFromSource("public class C { public void M() {} }", "M");
        var result = InvokeOrderMethodSymbols([methodSymbol]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(SymbolEqualityComparer.Default.Equals(result[0], methodSymbol), Is.True);
        }
    }

    [Test]
    public void HasValidDocumentationWithExampleReturnsTrue()
    {
        const string xml = "<member><example>This is an example.</example></member>";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void GetParamNamesReturnsCorrectNames()
    {
        const string xml = """<member><param name="p1">p1</param><param name="p2">p2</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1", "p2" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIgnoresParamWithoutName()
    {
        const string xml = """<member><param>no name</param><param name="p1">p1</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIncludesEmptyParam()
    {
        const string xml = """<member><param name="p1"> </param><param name="p2">p2</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1", "p2" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetParamNamesInvalidInputReturnsEmpty(string? xml)
    {
        Assert.That(DocumentationXmlExtensions.GetParamNames(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetTypeParamNamesInvalidInputReturnsEmpty(string? xml)
    {
        Assert.That(DocumentationXmlExtensions.GetTypeParamNames(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetExceptionCrefsInvalidInputReturnsEmpty(string? xml)
    {
        Assert.That(DocumentationXmlExtensions.GetExceptionCrefs(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void HasReturnsTagInvalidInputReturnsFalse(string? xml)
    {
        Assert.That(DocumentationXmlExtensions.HasReturnsTag(xml), Is.False);
    }

    [Test]
    public void HasAutoValidTagWithMemberElementDirectlyReturnsTrue()
    {
        var member = new XElement("member", new XElement("inheritdoc"));
        Assert.That(DocumentationXmlExtensions.HasAutoValidTag(member), Is.True);
    }

    [Test]
    public void GetParamNamesWithMemberElementDirectlyReturnsNames()
    {
        var member = new XElement("member", new XElement("param", new XAttribute("name", "x"), "Content"));
        var result = DocumentationXmlExtensions.GetParamNames(member).ToList();
        var expected = new[] { "x" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesWithMemberElementDirectlyReturnsNames()
    {
        var member = new XElement("member", new XElement("typeparam", new XAttribute("name", "T"), "Content"));
        var result = DocumentationXmlExtensions.GetTypeParamNames(member).ToList();
        var expected = new[] { "T" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetExceptionCrefsWithMemberElementDirectlyReturnsCrefs()
    {
        var member = new XElement("member", new XElement("exception", new XAttribute("cref", "Ex"), "Content"));
        var result = DocumentationXmlExtensions.GetExceptionCrefs(member).ToList();
        var expected = new[] { "Ex" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagWithMemberElementDirectlyReturnsTrue()
    {
        var member = new XElement("member", new XElement("returns", "Content"));
        Assert.That(DocumentationXmlExtensions.HasReturnsTag(member), Is.True);
    }

    [Test]
    public void TryParseDocumentationNullReturnsFalse()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation(null, out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationEmptyReturnsFalse()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation(string.Empty, out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationWhitespaceReturnsFalse()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation("   ", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationInvalidXmlReturnsFalse()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation("<invalid", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationValidXmlReturnsTrue()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation("<summary>Test</summary>", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(element.Descendants("summary").First().Value, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void HasAutoValidTagNestedInheritDocInSummaryReturnsFalse()
    {
        const string xml = "<summary><inheritdoc/></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationXmlExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void HasAutoValidTagNestedInheritDocInParaReturnsFalse()
    {
        const string xml = "<summary><para><inheritdoc/></para></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationXmlExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void GetParamNamesIgnoresNestedInSummary()
    {
        const string xml = """<member><summary>Use <param name="ignored"/> for something.</summary><param name="valid">Valid</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIgnoresNestedInRemarks()
    {
        const string xml = """<member><remarks>Use <param name="ignored"/> for something.</remarks><param name="valid">Valid</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagNestedInSummaryReturnsFalse()
    {
        const string xml = "<summary><returns>Not a return definition</returns></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationXmlExtensions.HasReturnsTag(root), Is.False);
        }
    }

    [Test]
    public void GetExceptionCrefsIgnoresNestedInSummary()
    {
        const string xml = """<member><summary>Throws <exception cref="IgnoredEx"/>.</summary><exception cref="ValidEx">Valid</exception></member>""";
        Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationXmlExtensions.GetExceptionCrefs(root).ToList();
        var expected = new[] { "ValidEx" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasAutoValidTagNestedInCustomTagReturnsFalse()
    {
        const string xml = "<mytag><inheritdoc/></mytag>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationXmlExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void GetParamNamesIgnoresNestedInCustomTag()
    {
        const string xml = """<member><mytag><param name="ignored">Nested</param></mytag><param name="valid">Valid</param></member>""";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesIgnoresNestedInSummary()
    {
        const string xml = """<member><summary><typeparam name="T">Ignored</typeparam></summary><typeparam name="U">Valid</typeparam></member>""";
        Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationXmlExtensions.GetTypeParamNames(root).ToList();
        var expected = new[] { "U" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagNestedInCustomTagReturnsFalse()
    {
        const string xml = "<mytag><returns>Not a return definition</returns></mytag>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationXmlExtensions.HasReturnsTag(root), Is.False);
        }
    }

    [Test]
    public void GetExceptionCrefsIgnoresNestedInCustomTag()
    {
        const string xml = """<member><mytag><exception cref="IgnoredEx">Nested</exception></mytag><exception cref="ValidEx">Valid</exception></member>""";
        Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationXmlExtensions.GetExceptionCrefs(root).ToList();
        var expected = new[] { "ValidEx" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesFindsParamInsideMemberWrapper()
    {
        const string xml = """<member><param name="x">Content</param></member>""";
        Assert.That(DocumentationXmlExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationXmlExtensions.GetParamNames(root).ToList();
        var expected = new[] { "x" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesStringOverloadReturnsNames()
    {
        const string xml = """<typeparam name="T">Test</typeparam>""";
        var result = DocumentationXmlExtensions.GetTypeParamNames(xml).ToList();
        var expected = new[] { "T" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagStringOverloadReturnsTrueWhenPresent()
    {
        const string xml = "<returns>Test</returns>";
        Assert.That(DocumentationXmlExtensions.HasReturnsTag(xml), Is.True);
    }

    [Test]
    public void GetExceptionCrefsStringOverloadReturnsCrefs()
    {
        const string xml = """<exception cref="T:System.Exception">Test</exception>""";
        var result = DocumentationXmlExtensions.GetExceptionCrefs(xml).ToList();
        var expected = new[] { "T:System.Exception" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasInheritDocPresentReturnsTrue()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationXmlExtensions.HasInheritDoc(root), Is.True);
    }

    [Test]
    public void HasInheritDocAbsentReturnsFalse()
    {
        var root = XElement.Parse("<root><summary/></root>");
        Assert.That(DocumentationXmlExtensions.HasInheritDoc(root), Is.False);
    }

    [Test]
    public void HasInheritDocWithCrefCrefPresentReturnsTrue()
    {
        var root = XElement.Parse("<root><inheritdoc cref='T:System.Object'/></root>");
        Assert.That(DocumentationXmlExtensions.HasInheritDocWithCref(root), Is.True);
    }

    [Test]
    public void HasInheritDocWithCrefCrefAbsentReturnsFalse()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationXmlExtensions.HasInheritDocWithCref(root), Is.False);
    }

    [Test]
    public void HasValueTagPresentReturnsTrue()
    {
        var root = XElement.Parse("<root><value>Test</value></root>");
        Assert.That(DocumentationXmlExtensions.HasValueTag(root), Is.True);
    }

    [Test]
    public void HasValueTagAbsentReturnsFalse()
    {
        var root = XElement.Parse("<root><summary/></root>");
        Assert.That(DocumentationXmlExtensions.HasValueTag(root), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithAutoValidTagReturnsTrue()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(root), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithUnknownTagReturnsFalse()
    {
        var root = XElement.Parse("<root><unknown/></root>");
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(root), Is.False);
    }

    [Test]
    public void GetTargetElementsWithTagNameReturnsOnlyMatching()
    {
        var root = XElement.Parse("<root><summary>S</summary><remarks>R</remarks></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root, "summary").ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Name.LocalName, Is.EqualTo("summary"));
        }
    }

    [Test]
    public void GetTargetElementsWithMemberElementReturnsDirectChildren()
    {
        var root = XElement.Parse("<member><summary>S</summary></member>");
        var result = DocumentationXmlExtensions.GetTargetElements(root).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Name.LocalName, Is.EqualTo("summary"));
        }
    }

    [Test]
    public void GetTagNameXmlElementReturnsCorrectName()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>Summary</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("summary"));
    }

    [Test]
    public void GetTagNameXmlEmptyElementReturnsCorrectName()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <inheritdoc />
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("inheritdoc"));
    }

    [Test]
    public void GetTagNameNonXmlNodeReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Summary
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.GetTagName(), Is.Empty);
    }

    [Test]
    public void GetNameAttributeXmlElementReturnsValue()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="x">P</param>
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("x"));
    }

    [Test]
    public void GetNameAttributeWithEntitiesReturnsDecodedValue()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <mytag name="x&amp;y">P</mytag>
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("x&y"));
    }

    [Test]
    public void GetNameAttributeXmlEmptyElementReturnsValue()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="y" />
            public void M(int y) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("y"));
    }

    [Test]
    public void GetNameAttributeMissingReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void GetNameAttributeNonXmlNodeReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Summary
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixWhitespaceReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            ///
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixPrefixReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary/>
            ///
            public class C {}
            """);
        var nodes = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().ToList();
        Assert.That(nodes, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(nodes[1].IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixTextReturnsFalse()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Some text
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixNullReturnsFalse()
    {
        Assert.That(((XmlTextSyntax?)null).IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixEmptyStringReturnsTrue()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(string.Empty)));
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixOnlySlashReturnsFalse()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("/")));
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void GetElementAttributeValuesTopLevelOnlyFalseReturnsDeepValues()
    {
        var xml = """<member><summary><param name="inner">Inner</param></summary><param name="outer">Outer</param></member>""";
        var root = XElement.Parse(xml);
        var result = DocumentationXmlExtensions.GetElementAttributeValues(root, "param", "name", topLevelOnly: false).ToList();
        var expected = new[] { "inner", "outer" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetElementAttributeValuesTopLevelOnlyTrueReturnsOnlyDirectValues()
    {
        var xml = """<member><summary><param name="inner">Inner</param></summary><param name="outer">Outer</param></member>""";
        var root = XElement.Parse(xml);
        var result = DocumentationXmlExtensions.GetElementAttributeValues(root, "param", "name", topLevelOnly: true).ToList();
        var expected = new[] { "outer" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveAtStartReturnsTrailing()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="x" />
            /// <summary>S</summary>
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Does.Contain("\n"));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveNotAtStartReturnsLeading()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary>
            /// <param name="x" />
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Does.Contain("\n"));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveNotAtEndReturnsTrailing()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary> <param name="x" /> <returns>R</returns>
            public int M(int x) => 0;
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Is.EqualTo(" "));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveNoWhitespaceReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary><param name="x"/><returns>R</returns>
            public int M(int x) => 0;
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveAtStartWithPrefixReturnsTrailing()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="x" />
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();

        var trivia = node.FirstAncestorOrSelf<DocumentationCommentTriviaSyntax>() ?? throw new InvalidOperationException();
        var index = trivia.Content.IndexOf(node);
        Assert.That(index, Is.EqualTo(1));

        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Does.Contain("\n"));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveInMiddleReturnsLeading()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary>
            /// <param name="x" />
            /// <returns>R</returns>
            public int M(int x) => 0;
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();

        var result = node.GetAssociatedWhitespaceToRemove();

        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Does.Contain("\n"));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveUnsupportedParentReturnsNull()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")));
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveTrailingIsNullReturnsNull()
    {
        var summary = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("summary"));
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("root"),
            SyntaxFactory.List(new XmlNodeSyntax[] {
                SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("Text"))),
                summary
            }));

        var node = element.Content[1];
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveLeadingIsNullReturnsNull()
    {
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("summary"),
            SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("S")))));
        var node = element.Content[0];
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveIndexOneAndLeadingNotXmlTextReturnsNull()
    {
        var param = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param"));
        var summary = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("summary"));
        var root = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("root"),
            SyntaxFactory.List(new XmlNodeSyntax[] { summary, param }));

        var node = root.Content[1]; // <param/>
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveIndexOneAndLeadingNotPureWhitespaceReturnsNull()
    {
        var param = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param"));
        var summary = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("summary"),
            SyntaxFactory.List(new XmlNodeSyntax[] {
                SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("Text"))),
                param
            }));

        var node = summary.Content[1];
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveTrailingNotAtEndAndLeadingNotPureReturnsTrailing()
    {
        var param = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param"));
        var returns = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("returns"));
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("summary"),
            SyntaxFactory.List(new XmlNodeSyntax[] {
                SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("Text "))),
                param,
                SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(" "))),
                returns
            }));

        var node = element.Content[1]; // <param/>
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Is.EqualTo(" "));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveIndexZeroAndTrailingPureReturnsTrailing()
    {
        var param = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("param"));
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("summary"),
            SyntaxFactory.List(new XmlNodeSyntax[] {
                param,
                SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(" ")))
            }));

        var node = element.Content[0];
        var result = node.GetAssociatedWhitespaceToRemove();
        Assert.That(result, Is.Not.Null);
        Assert.That(result.ToString(), Is.EqualTo(" "));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveIndexIsMinusOneReturnsNull()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")));
        var emptyList = SyntaxFactory.List<XmlNodeSyntax>();

        var result = DocumentationSyntaxExtensions.GetAssociatedWhitespaceToRemove(node, emptyList);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetParentContentTopLevelNodeReturnsDocTrivia()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.InstanceOf<DocumentationCommentTriviaSyntax>());
            Assert.That(content, Is.Not.Empty);
        }
    }

    [Test]
    public void GetParentContentNestedNodeReturnsElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary><see cref="T"/></summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.InstanceOf<XmlElementSyntax>());
            Assert.That(content, Is.Not.Empty);
        }
    }

    [Test]
    public void GetParentContentTextNodeWalksUpToElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>Text</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(t => t.ToString() == "Text");
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.InstanceOf<XmlElementSyntax>());
            Assert.That(content, Is.Not.Empty);
        }
    }

    [Test]
    public void GetParentContentDetachedNodeReturnsNull()
    {
        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("summary"));
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Null);
            Assert.That(content, Is.Default);
        }
    }

    [Test]
    public void GetTargetElementsRecursiveReturnsAllMatching()
    {
        var root = XElement.Parse("<root><summary><see cref='T1'/></summary><see cref='T2'/></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root, "see", recursive: true).ToList();
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetDocumentationLocationsRecursiveReturnsDeepLocations()
    {
        const string source = """
            public class Test
            {
                /// <summary><param name="p1">Inner</param></summary>
                public void Method(int p1) { }
            }
            """;
        var symbol = GetSymbolFromSource(source, "Method");
        var locations = symbol.GetDocumentationLocations("param", topLevelOnly: false);
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetDocumentationLocationCrefWithTPrefixReturnsLocation()
    {
        const string source = """
            public class Test
            {
                /// <exception cref="System.Exception">Docs</exception>
                public void Method() { }
            }
            """;
        var symbol = GetSymbolFromSource(source, "Method");
        var location = symbol.GetDocumentationLocation("exception", "T:System.Exception", attributeName: "cref");
        Assert.That(location, Is.Not.EqualTo(Location.None));
    }

    private static ISymbol GetSymbolFromSource(string source, string symbolName)
    {
        return RoslynTestUtils.GetSymbolFromSource(source, symbolName, parseDocumentation: true);
    }

    private static int InvokeGetMethodDeclarationOrder(IMethodSymbol methodSymbol)
    {
        var method = typeof(DocumentationComment).GetMethod("GetMethodDeclarationOrder", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                     ?? throw new InvalidOperationException("Could not find GetMethodDeclarationOrder.");

        return (int)(method.Invoke(null, [methodSymbol]) ?? throw new InvalidOperationException("GetMethodDeclarationOrder returned null."));
    }

    private static void InvokeAddIfMissing(List<IMethodSymbol> methods, IMethodSymbol candidate)
    {
        var method = typeof(DocumentationComment).GetMethod("AddIfMissing", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                     ?? throw new InvalidOperationException("Could not find AddIfMissing.");

        method.Invoke(null, [methods, candidate]);
    }

    private static List<IMethodSymbol> InvokeOrderMethodSymbols(List<IMethodSymbol> methods)
    {
        var method = typeof(DocumentationComment).GetMethod("OrderMethodSymbols", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)
                     ?? throw new InvalidOperationException("Could not find OrderMethodSymbols.");

        return (List<IMethodSymbol>)(method.Invoke(null, [methods]) ?? throw new InvalidOperationException("OrderMethodSymbols returned null."));
    }

    private static Mock<IMethodSymbol> CreateMethodSymbolMock(ITypeSymbol returnType, ImmutableArray<IParameterSymbol>? parameters = null)
    {
        var method = new Mock<IMethodSymbol>();
        method.SetupGet(symbol => symbol.MethodKind).Returns(MethodKind.Ordinary);
        method.SetupGet(symbol => symbol.Arity).Returns(0);
        method.SetupGet(symbol => symbol.ReturnType).Returns(returnType);
        method.SetupGet(symbol => symbol.Parameters).Returns(parameters ?? ImmutableArray<IParameterSymbol>.Empty);
        return method;
    }

    private static Mock<IParameterSymbol> CreateParameterSymbolMock(ITypeSymbol type)
    {
        var parameter = new Mock<IParameterSymbol>();
        parameter.SetupGet(symbol => symbol.Type).Returns(type);
        return parameter;
    }

    private static CSharpCompilation CreateCompilation()
    {
        return CSharpCompilation.Create(
            "TestAssembly",
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
    }

    private static ISymbol GetSymbolFromSources((string FilePath, string Source) first, (string FilePath, string Source) second, string symbolName)
    {
        var parseOptions = new CSharpParseOptions(
            languageVersion: LanguageVersion.Latest,
            documentationMode: DocumentationMode.Parse);

        var firstTree = CSharpSyntaxTree.ParseText(first.Source, parseOptions, path: first.FilePath);
        var secondTree = CSharpSyntaxTree.ParseText(second.Source, parseOptions, path: second.FilePath);

        var compilation = CSharpCompilation.Create(
                "TestAssembly",
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, allowUnsafe: true))
            .AddReferences(AppDomain.CurrentDomain.GetAssemblies()
                .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
                .Select(a => MetadataReference.CreateFromFile(a.Location)))
            .AddSyntaxTrees(firstTree, secondTree);

        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var errors = string.Join(Environment.NewLine, diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Fail($"Compilation failed:{Environment.NewLine}{errors}");
        }

        foreach (var syntaxTree in compilation.SyntaxTrees)
        {
            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var declaration = syntaxTree.GetRoot().DescendantNodes()
                .OfType<BaseTypeDeclarationSyntax>()
                .FirstOrDefault(node => node.Identifier.ValueText == symbolName);

            if (declaration == null)
                continue;

            var symbol = semanticModel.GetDeclaredSymbol(declaration);
            if (symbol != null)
                return symbol;
        }

        throw new InvalidOperationException($"Could not find symbol for '{symbolName}' in the provided source code.");
    }
}
