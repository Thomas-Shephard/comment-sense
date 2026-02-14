using CommentSense.Core.Utilities;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class StringExtensionsTests
{
    [Test]
    public void CalculateSimilarityIdentityReturnsOne()
    {
        var result = "Same".CalculateSimilarity("Same");
        Assert.That(result, Is.EqualTo(1.0));
    }

    [Test]
    public void CalculateSimilarityDifferentReturnsValue()
    {
        var result = "ABC".CalculateSimilarity("ABD");
        Assert.That(result, Is.LessThan(1.0).And.GreaterThan(0.0));
    }

    [Test]
    public void ComputeLevenshteinDistanceSwapBranch()
    {
        var result = "A".CalculateSimilarity("BB");
        Assert.That(result, Is.Zero);
    }

    [Test]
    public void LevenshteinDistanceHeapAllocationBranch()
    {
        var longSource = new string('A', 300);
        var longTarget = new string('A', 290) + "BBBBBBBBBB";

        var result = longSource.CalculateSimilarity(longTarget);
        Assert.That(result, Is.GreaterThan(0.95));
    }
}
