using System.Xml.Linq;
using CommentSense.Analyzers.Logic;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class QualityAnalyzerTests
{
    [Test]
    public void IsLowQualityDifferentTextReturnsFalse()
    {
        var element = new XElement("summary", "Some text that is not the symbol name");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityEmptyElementReturnsTrue()
    {
        var element = new XElement("summary");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWhitespaceElementReturnsTrue()
    {
        var element = new XElement("summary", "   ");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameReturnsTrue()
    {
        var element = new XElement("summary", "MySymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameCaseInsensitiveReturnsTrue()
    {
        var element = new XElement("summary", "mysymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWithNestedElementsReturnsFalse()
    {
        var element = new XElement("summary", new XElement("see", new XAttribute("cref", "T:System.Object")));
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityInternalNullContentReturnsTrue()
    {
        var result = QualityAnalyzer.IsLowQuality((string?)null, "Symbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityInternalWhitespaceContentReturnsTrue()
    {
        var result = QualityAnalyzer.IsLowQuality("   ", "Symbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CalculateSimilarityIdentityReturnsOne()
    {
        var result = QualityAnalyzer.CalculateSimilarity("Same", "Same");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateSimilarityDifferentReturnsValue()
    {
        var result = QualityAnalyzer.CalculateSimilarity("ABC", "ABD");
        Assert.That(result, Is.LessThan(1.0).And.GreaterThan(0.0));
    }

    [Test]
    public void IsLowQualityReturnsBranch()
    {
        var element = new XElement("returns", "return");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default, tagName: "returns");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityReturnsKeywordBranch()
    {
        var element = new XElement("returns", "returns");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default, tagName: "returns");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LevenshteinDistanceHeapAllocationBranch()
    {
        var longSymbolName = new string('A', 300);
        var longSummary = new string('A', 290) + "BBBBBBBBBB";

        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.9 };
        var element = new XElement("summary", longSummary);

        var result = QualityAnalyzer.IsLowQuality(element, longSymbolName, options);
        Assert.That(result, Is.True);
    }

    [Test]
    public void QualityAnalyzerNormalizationToEmptyReturnsTrue()
    {
        var options = CommentSenseOptions.Default with { RequireEndingPunctuation = true };
        var element = new XElement("summary", "...");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", options);
        Assert.That(result, Is.True);
    }

    [Test]
    public void ComputeLevenshteinDistanceSwapBranch()
    {
        var result = QualityAnalyzer.CalculateSimilarity("A", "BB");
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void CalculateSimilarityBelowThreshold()
    {
        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.9 };
        var result = QualityAnalyzer.IsLowQuality(new XElement("summary", "Different"), "Symbol", options);
        Assert.That(result, Is.False);
    }
}
