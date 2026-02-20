using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationLocationCacheTests
{
    private const string TestSource = """
        public class TestClass
        {
            /// <summary>S1</summary>
            /// <summary>S2</summary>
            /// <param name="x">X</param>
            public void M(int x) { }
        }
        """;

    [Test]
    public void GetLocationsReturnsLocationsAndCaches()
    {
        var cache = new DocumentationLocationCache();
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);

        var locations1 = cache.GetLocations(symbol, "summary");
        Assert.That(locations1, Has.Length.EqualTo(2));

        var locations2 = cache.GetLocations(symbol, "summary");
        Assert.That(locations2, Is.EqualTo(locations1));
    }

    [Test]
    public void GetLocationValidOccurrenceReturnsLocation()
    {
        var cache = new DocumentationLocationCache();
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);

        var loc0 = cache.GetLocation(symbol, "summary");
        var loc1 = cache.GetLocation(symbol, "summary", 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(loc0, Is.Not.EqualTo(Location.None));
            Assert.That(loc1, Is.Not.EqualTo(Location.None));
            Assert.That(loc0, Is.Not.EqualTo(loc1));
        }
    }

    [Test]
    public void GetLocationNegativeOccurrenceReturnsSymbolLocation()
    {
        var cache = new DocumentationLocationCache();
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);

        var loc = cache.GetLocation(symbol, "summary", -1);
        Assert.That(loc, Is.EqualTo(DocumentationLocationExtensions.GetSymbolLocation(symbol)));
    }

    [Test]
    public void GetLocationOccurrenceTooLargeReturnsSymbolLocation()
    {
        var cache = new DocumentationLocationCache();
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);

        var loc = cache.GetLocation(symbol, "summary", 2);
        Assert.That(loc, Is.EqualTo(DocumentationLocationExtensions.GetSymbolLocation(symbol)));
    }

    [Test]
    public void GetLocationMissingTagReturnsSymbolLocation()
    {
        var cache = new DocumentationLocationCache();
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);

        var loc = cache.GetLocation(symbol, "nonexistent");
        Assert.That(loc, Is.EqualTo(DocumentationLocationExtensions.GetSymbolLocation(symbol)));
    }

    [Test]
    public void GetLocationWithAttributeReturnsLocation()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);
        var loc = DocumentationLocationCache.GetLocationWithAttribute(symbol, "param", "x");
        Assert.That(loc, Is.Not.EqualTo(Location.None));
    }

    [Test]
    public void GetLocationWithAttributeMismatchedValueReturnsSymbolLocation()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(TestSource, "M", parseDocumentation: true);
        var loc = DocumentationLocationCache.GetLocationWithAttribute(symbol, "param", "y");
        Assert.That(loc, Is.EqualTo(DocumentationLocationExtensions.GetSymbolLocation(symbol)));
    }
}
