using System.Xml.Linq;
using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
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
}
