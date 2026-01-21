using Microsoft.CodeAnalysis;
using CommentSense.Utilities;
using NUnit.Framework;

namespace CommentSense.Tests.Utilities;

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
            /// This is a test class.
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
                /// <param name="x">The x.</param>
                public void M(int x) {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithEmptySummaryReturnsFalse()
    {
        const string source = """
            /// <summary>
            /// </summary>
            public class TestClass {}
            """;
        var symbol = GetSymbolFromSource(source, "TestClass");
        Assert.That(symbol.HasValidDocumentation(), Is.False);
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
    public void HasValidDocumentationWithEmptyParamReturnsFalse()
    {
        const string source = """
            public class C {
                /// <param name="x"></param>
                public void M(int x) {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithEmptyExceptionReturnsFalse()
    {
        const string source = """
            public class C {
                /// <exception cref="System.Exception"></exception>
                public void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.False);
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
    public void HasValidDocumentationWithEmptyExceptionXmlReturnsFalse()
    {
        const string xml = """<member><exception cref="T:System.Exception"/></member>""";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.False);
    }

    [Test]
    public void HasValidDocumentationWithEmptyTagsReturnsFalse()
    {
        const string xml = "<member><summary> </summary><remarks/></member>";
        Assert.That(DocumentationExtensions.HasValidDocumentation(xml), Is.False);
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
    public void GetParamNamesIgnoresEmptyParam()
    {
        const string xml = """<member><param name="p1"> </param><param name="p2">p2</param></member>""";
        var result = DocumentationExtensions.GetParamNames(xml).ToList();
        var expected = new[] { "p2" };
        Assert.That(result, Is.EquivalentTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    public void GetParamNamesReturnsEmptyForNullOrEmpty(string? xml)
    {
        Assert.That(DocumentationExtensions.GetParamNames(xml), Is.Empty);
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
}
