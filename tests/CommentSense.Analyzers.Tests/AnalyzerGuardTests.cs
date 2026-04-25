using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class AnalyzerGuardTests
{
    [Test]
    public void AgainstNullReturnsValue()
    {
        const string value = "ok";

        var result = AnalyzerGuard.AgainstNull(value);

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void AgainstNullThrowsForNull()
    {
        Assert.Throws<InvalidOperationException>(() => AnalyzerGuard.AgainstNull<string>(null));
    }
}
