using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
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
            public class BaseClass {
                /// <summary>Base method</summary>
                public virtual void M() {}
            }

            public class DerivedClass : BaseClass {
                /// <inheritdoc />
                public override void M() {}
            }
            """;
        var symbol = GetSymbolFromSource(source, "M");
        Assert.That(symbol.HasValidDocumentation(), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithNestedInheritDocReturnsTrue()
    {
        const string source = """
            public class BaseClass {
                /// <summary>Base method</summary>
                public virtual void M() {}
            }

            public class DerivedClass : BaseClass {
                /// <summary><inheritdoc /></summary>
                public override void M() {}
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
        Assert.That(DocumentationExtensions.HasValidDocumentation(null), Is.False);
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
}
