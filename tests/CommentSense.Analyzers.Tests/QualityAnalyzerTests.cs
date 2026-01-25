using System.Xml.Linq;
using CommentSense.Analyzers.Logic;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class QualityAnalyzerTests
{
    [Test]
    public void IsLowQualityNullKeywordsReturnsFalse()
    {
        var element = new XElement("summary", "Some text that is not the symbol name");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityEmptyElementReturnsTrue()
    {
        var element = new XElement("summary");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWhitespaceElementReturnsTrue()
    {
        var element = new XElement("summary", "   ");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameReturnsTrue()
    {
        var element = new XElement("summary", "MySymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameCaseInsensitiveReturnsTrue()
    {
        var element = new XElement("summary", "mysymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWithNestedElementsReturnsFalse()
    {
        var element = new XElement("summary", new XElement("see", new XAttribute("cref", "T:System.Object")));
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol");
        Assert.That(result, Is.False);
    }
}
