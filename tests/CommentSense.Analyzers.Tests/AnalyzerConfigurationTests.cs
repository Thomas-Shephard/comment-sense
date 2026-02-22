using System.Collections.Immutable;
using NUnit.Framework;
using CommentSense.Analyzers.Logic;

namespace CommentSense.Analyzers.Tests;

public class AnalyzerConfigurationTests
{
    [Test]
    public void NameListComparerWorksCorrectly()
    {
        var comparer = new GhostReferenceAnalyzer.NameListComparer();

        var array1 = ImmutableArray.Create("a", "b");
        var array2 = ImmutableArray.Create("a", "b");
        var array3 = ImmutableArray.Create("a", "B");
        var array4 = ImmutableArray.Create("a");
        var array5 = ImmutableArray.Create("a", "c");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comparer.Equals(array1, array2), Is.True);
            Assert.That(comparer.GetHashCode(array1), Is.EqualTo(comparer.GetHashCode(array2)));

            Assert.That(comparer.Equals(array1, array3), Is.True);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(comparer.GetHashCode(array1), Is.EqualTo(comparer.GetHashCode(array3)));

            Assert.That(comparer.Equals(array1, array4), Is.False);

            Assert.That(comparer.Equals(array1, array5), Is.False);
        }
    }
}
