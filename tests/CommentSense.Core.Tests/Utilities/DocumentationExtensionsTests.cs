using System.Xml.Linq;
using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationExtensionsTests
{
    private static ISymbol GetSymbolFromSource(string source, string symbolName)
    {
        return RoslynTestUtils.GetSymbolFromSource(source, symbolName, parseDocumentation: true);
    }

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
    public void HasValidDocumentationReturnsFalseForNull()
    {
        Assert.That(((ISymbol?)null).HasValidDocumentation(), Is.False);
    }

    [Test]
    public void HasValidDocumentationReturnsFalseForNullString()
    {
        Assert.That(DocumentationExtensions.HasValidDocumentation((string?)null), Is.False);
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
    public void HasValidDocumentationReturnsFalseForMalformedXml()
    {
        const string xml = "<invalid";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithIncludeTagReturnsTrue()
    {
        const string xml = """<member><include file='docs.xml' path='[@name="test"]'/></member>""";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithNestedElementsInSummaryReturnsTrue()
    {
        const string xml = """<member><summary><see cref="T:System.String"/></summary></member>""";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyExceptionXmlReturnsTrue()
    {
        const string xml = """<member><exception cref="T:System.Exception"/></member>""";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptyTagsReturnsTrue()
    {
        const string xml = "<member><summary> </summary><remarks/></member>";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithTypeParamReturnsTrue()
    {
        const string xml = """<member><typeparam name="T">The type.</typeparam></member>""";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithExampleReturnsTrue()
    {
        const string xml = "<member><example>This is an example.</example></member>";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void GetParamNamesReturnsNames()
    {
        const string xml = """<member><param name="p1">p1</param><param name="p2">p2</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1", "p2" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIgnoresParamWithoutName()
    {
        const string xml = """<member><param>no name</param><param name="p1">p1</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIncludesEmptyParam()
    {
        const string xml = """<member><param name="p1"> </param><param name="p2">p2</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p1", "p2" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetParamNamesReturnsEmptyForInvalidInput(string? xml)
    {
        Assert.That(DocumentationExtensions.GetParamNames(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetTypeParamNamesReturnsEmptyForInvalidInput(string? xml)
    {
        Assert.That(DocumentationExtensions.GetTypeParamNames(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void GetExceptionCrefsReturnsEmptyForInvalidInput(string? xml)
    {
        Assert.That(DocumentationExtensions.GetExceptionCrefs(xml), Is.Empty);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    [TestCase("<invalid")]
    public void HasReturnsTagReturnsFalseForInvalidInput(string? xml)
    {
        Assert.That(DocumentationExtensions.HasReturnsTag(xml), Is.False);
    }

    [Test]
    public void HasAutoValidTagWithMemberElementDirectly()
    {
        var member = new XElement("member", new XElement("inheritdoc"));
        Assert.That(DocumentationExtensions.HasAutoValidTag(member), Is.True);
    }

    [Test]
    public void GetParamNamesWithMemberElementDirectly()
    {
        var member = new XElement("member", new XElement("param", new XAttribute("name", "x"), "Content"));
        var result = DocumentationExtensions.GetParamNames(member).ToList();
        var expected = new[] { "x" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesWithMemberElementDirectly()
    {
        var member = new XElement("member", new XElement("typeparam", new XAttribute("name", "T"), "Content"));
        var result = DocumentationExtensions.GetTypeParamNames(member).ToList();
        var expected = new[] { "T" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetExceptionCrefsWithMemberElementDirectly()
    {
        var member = new XElement("member", new XElement("exception", new XAttribute("cref", "Ex"), "Content"));
        var result = DocumentationExtensions.GetExceptionCrefs(member).ToList();
        var expected = new[] { "Ex" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagWithMemberElementDirectly()
    {
        var member = new XElement("member", new XElement("returns", "Content"));
        Assert.That(DocumentationExtensions.HasReturnsTag(member), Is.True);
    }

    [Test]
    public void TryParseDocumentationReturnsFalseForNull()
    {
        var result = DocumentationExtensions.TryParseDocumentation(null, out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationReturnsFalseForEmpty()
    {
        var result = DocumentationExtensions.TryParseDocumentation(string.Empty, out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationReturnsFalseForWhitespace()
    {
        var result = DocumentationExtensions.TryParseDocumentation("   ", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationReturnsFalseForInvalidXml()
    {
        var result = DocumentationExtensions.TryParseDocumentation("<invalid", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void TryParseDocumentationReturnsTrueForValidXml()
    {
        var result = DocumentationExtensions.TryParseDocumentation("<summary>Test</summary>", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(element.Descendants("summary").First().Value, Is.EqualTo("Test"));
        }
    }

    [Test]
    public void HasAutoValidTagReturnsFalseWhenInheritDocIsNestedInSummary()
    {
        const string xml = "<summary><inheritdoc/></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void HasAutoValidTagReturnsFalseWhenInheritDocIsNestedInParaInSummary()
    {
        const string xml = "<summary><para><inheritdoc/></para></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void GetParamNamesIgnoresParamNestedInSummary()
    {
        const string xml = """<member><summary>Use <param name="ignored"/> for something.</summary><param name="valid">Valid</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesIgnoresParamNestedInRemarks()
    {
        const string xml = """<member><remarks>Use <param name="ignored"/> for something.</remarks><param name="valid">Valid</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagReturnsFalseWhenReturnsIsNestedInSummary()
    {
        const string xml = "<summary><returns>Not a return definition</returns></summary>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationExtensions.HasReturnsTag(root), Is.False);
        }
    }

    [Test]
    public void GetExceptionCrefsIgnoresExceptionNestedInSummary()
    {
        const string xml = """<member><summary>Throws <exception cref="IgnoredEx"/>.</summary><exception cref="ValidEx">Valid</exception></member>""";
        Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationExtensions.GetExceptionCrefs(root).ToList();
        var expected = new[] { "ValidEx" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasAutoValidTagReturnsFalseWhenNestedInCustomTag()
    {
        const string xml = "<mytag><inheritdoc/></mytag>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationExtensions.HasAutoValidTag(root), Is.False);
        }
    }

    [Test]
    public void GetParamNamesIgnoresParamNestedInCustomTag()
    {
        const string xml = """<member><mytag><param name="ignored">Nested</param></mytag><param name="valid">Valid</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "valid" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesIgnoresTypeParamNestedInSummary()
    {
        const string xml = """<member><summary><typeparam name="T">Ignored</typeparam></summary><typeparam name="U">Valid</typeparam></member>""";
        Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationExtensions.GetTypeParamNames(root).ToList();
        var expected = new[] { "U" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagReturnsFalseWhenNestedInCustomTag()
    {
        const string xml = "<mytag><returns>Not a return definition</returns></mytag>";
        using (Assert.EnterMultipleScope())
        {
            Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
            Assert.That(DocumentationExtensions.HasReturnsTag(root), Is.False);
        }
    }

    [Test]
    public void GetExceptionCrefsIgnoresExceptionNestedInCustomTag()
    {
        const string xml = """<member><mytag><exception cref="IgnoredEx">Nested</exception></mytag><exception cref="ValidEx">Valid</exception></member>""";
        Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationExtensions.GetExceptionCrefs(root).ToList();
        var expected = new[] { "ValidEx" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetParamNamesFindsParamInsideMemberWrapper()
    {
        const string xml = """<member><param name="x">Content</param></member>""";
        Assert.That(DocumentationExtensions.TryParseDocumentation(xml, out var root), Is.True);
        var result = DocumentationExtensions.GetParamNames(root).ToList();
        var expected = new[] { "x" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetTypeParamNamesStringOverloadReturnsNames()
    {
        const string xml = """<typeparam name="T">Test</typeparam>""";
        var result = DocumentationExtensions.GetTypeParamNames(xml).ToList();
        var expected = new[] { "T" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasReturnsTagStringOverloadReturnsTrueWhenReturnsTagIsPresent()
    {
        const string xml = "<returns>Test</returns>";
        Assert.That(DocumentationExtensions.HasReturnsTag(xml), Is.True);
    }

    [Test]
    public void GetExceptionCrefsStringOverloadReturnsCrefs()
    {
        const string xml = """<exception cref="T:System.Exception">Test</exception>""";
        var result = DocumentationExtensions.GetExceptionCrefs(xml).ToList();
        var expected = new[] { "T:System.Exception" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void HasInheritDocReturnsTrueWhenPresent()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationExtensions.HasInheritDoc(root), Is.True);
    }

    [Test]
    public void HasInheritDocReturnsFalseWhenAbsent()
    {
        var root = XElement.Parse("<root><summary/></root>");
        Assert.That(DocumentationExtensions.HasInheritDoc(root), Is.False);
    }

    [Test]
    public void HasInheritDocWithCrefReturnsTrueWhenCrefPresent()
    {
        var root = XElement.Parse("<root><inheritdoc cref='T:System.Object'/></root>");
        Assert.That(DocumentationExtensions.HasInheritDocWithCref(root), Is.True);
    }

    [Test]
    public void HasInheritDocWithCrefReturnsFalseWhenCrefAbsent()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationExtensions.HasInheritDocWithCref(root), Is.False);
    }

    [Test]
    public void HasValueTagReturnsTrueWhenPresent()
    {
        var root = XElement.Parse("<root><value>Test</value></root>");
        Assert.That(DocumentationExtensions.HasValueTag(root), Is.True);
    }

    [Test]
    public void HasValueTagReturnsFalseWhenAbsent()
    {
        var root = XElement.Parse("<root><summary/></root>");
        Assert.That(DocumentationExtensions.HasValueTag(root), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithAutoValidTagReturnsTrue()
    {
        var root = XElement.Parse("<root><inheritdoc/></root>");
        Assert.That(DocumentationExtensions.HasValidDocumentation(root), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithUnknownTagReturnsFalse()
    {
        var root = XElement.Parse("<root><unknown/></root>");
        Assert.That(DocumentationExtensions.HasValidDocumentation(root), Is.False);
    }

    [Test]
    public void GetTargetElementsWithTagNameReturnsOnlyMatching()
    {
        var root = XElement.Parse("<root><summary>S</summary><remarks>R</remarks></root>");
        var result = DocumentationExtensions.GetTargetElements(root, "summary").ToList();
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
        var result = DocumentationExtensions.GetTargetElements(root).ToList();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Has.Count.EqualTo(1));
            Assert.That(result[0].Name.LocalName, Is.EqualTo("summary"));
        }
    }

    [Test]
    public void GetTagNameReturnsCorrectNameForElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>Summary</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("summary"));
    }

    [Test]
    public void GetTagNameReturnsCorrectNameForEmptyElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <inheritdoc />
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetTagName(), Is.EqualTo("inheritdoc"));
    }

    [Test]
    public void GetTagNameReturnsEmptyForNonXmlNode()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Summary
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        // XmlTextSyntax is a XmlNodeSyntax but not XmlElement/XmlEmptyElement
        Assert.That(node.GetTagName(), Is.Empty);
    }

    [Test]
    public void GetNameAttributeReturnsCorrectValueForElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="x">P</param>
            public void M(int x) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("x"));
    }

    [Test]
    public void GetNameAttributeReturnsCorrectValueForEmptyElement()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <param name="y" />
            public void M(int y) {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlEmptyElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.EqualTo("y"));
    }

    [Test]
    public void GetNameAttributeReturnsNullWhenMissing()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary>S</summary>
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlElementSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void GetNameAttributeReturnsNullForNonXmlNode()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Summary
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.GetNameAttribute(), Is.Null);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsTrueForWhitespace()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            ///
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsTrueForPrefix()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// <summary/>
            ///
            public class C {}
            """);
        var nodes = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().ToList();
        // nodes[0] is "/// "
        // nodes[1] is "\n/// "
        Assert.That(nodes, Has.Count.GreaterThanOrEqualTo(2));
        Assert.That(nodes[1].IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsFalseForText()
    {
        var tree = CSharpSyntaxTree.ParseText("""
            /// Some text
            public class C {}
            """);
        var node = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First();
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsFalseForNull()
    {
        Assert.That(((XmlTextSyntax?)null).IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsTrueForEmptyString()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(string.Empty)));
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.True);
    }

    [Test]
    public void IsPureWhitespaceOrPrefixReturnsFalseForOnlySlash()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("/")));
        Assert.That(node.IsPureWhitespaceOrPrefix(), Is.False);
    }

    [Test]
    public void GetElementAttributeValuesWithTopLevelOnlyFalse()
    {
        var xml = """<member><summary><param name="inner">Inner</param></summary><param name="outer">Outer</param></member>""";
        var root = XElement.Parse(xml);
        var result = DocumentationExtensions.GetElementAttributeValues(root, "param", "name", topLevelOnly: false).ToList();
        var expected = new[] { "inner", "outer" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetElementAttributeValuesWithTopLevelOnlyTrue()
    {
        var xml = """<member><summary><param name="inner">Inner</param></summary><param name="outer">Outer</param></member>""";
        var root = XElement.Parse(xml);
        var result = DocumentationExtensions.GetElementAttributeValues(root, "param", "name", topLevelOnly: true).ToList();
        var expected = new[] { "outer" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveReturnsTrailingWhenAtStart()
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
    public void GetAssociatedWhitespaceToRemoveReturnsLeadingWhenNotAtStart()
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
    public void GetAssociatedWhitespaceToRemoveReturnsTrailingWhenNotAtEnd()
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
    public void GetAssociatedWhitespaceToRemoveReturnsNullWhenNoWhitespace()
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
    public void GetAssociatedWhitespaceToRemoveReturnsTrailingWhenAtStartWithPrefix()
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
    public void GetAssociatedWhitespaceToRemoveReturnsLeadingWhenInMiddle()
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
    public void GetAssociatedWhitespaceToRemoveReturnsNullForUnsupportedParent()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")));
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveReturnsNullWhenTrailingIsNull()
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
    public void GetAssociatedWhitespaceToRemoveReturnsNullWhenLeadingIsNull()
    {
        var element = SyntaxFactory.XmlElement(
            SyntaxFactory.XmlName("summary"),
            SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("S")))));
        var node = element.Content[0];
        Assert.That(node.GetAssociatedWhitespaceToRemove(), Is.Null);
    }

    [Test]
    public void GetAssociatedWhitespaceToRemoveWithIndexOneAndLeadingNotXmlText()
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
    public void GetAssociatedWhitespaceToRemoveWithIndexOneAndLeadingNotPureWhitespace()
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
    public void GetAssociatedWhitespaceToRemoveReturnsTrailingWhenNotAtEndAndLeadingNotPure()
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
    public void GetAssociatedWhitespaceToRemoveWithIndexZeroAndTrailingPure()
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
    public void GetAssociatedWhitespaceToRemoveReturnsNullWhenIndexIsMinusOne()
    {
        var node = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")));
        var emptyList = SyntaxFactory.List<XmlNodeSyntax>();

        var result = DocumentationExtensions.GetAssociatedWhitespaceToRemove(node, emptyList);
        Assert.That(result, Is.Null);
    }

    [Test]
    public void GetParentContentReturnsDocTriviaForTopLevelNode()
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
    public void GetParentContentReturnsElementForNestedNode()
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
    public void GetParentContentWalksUpToElement()
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
    public void GetParentContentReturnsNullForDetachedNode()
    {
        var node = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("summary"));
        var (parent, content) = node.GetParentContent();
        using (Assert.EnterMultipleScope())
        {
            Assert.That(parent, Is.Null);
            Assert.That(content, Is.Default);
        }
    }
}
