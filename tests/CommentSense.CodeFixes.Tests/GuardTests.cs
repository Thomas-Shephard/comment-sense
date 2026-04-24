using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class GuardTests
{
    [Test]
    public void AgainstNullReturnsValue()
    {
        const string value = "ok";

        var result = Guard.AgainstNull(value);

        Assert.That(result, Is.EqualTo(value));
    }

    [Test]
    public void AgainstNullThrowsForNull()
    {
        Assert.Throws<InvalidOperationException>(() => Guard.AgainstNull<string>(null));
    }

    [Test]
    public void WhenNotNullExecutesCallback()
    {
        var result = Guard.WhenNotNull("ok", value => value.Length, -1);

        Assert.That(result, Is.EqualTo(2));
    }

    [Test]
    public void WhenNotNullReturnsFallbackForNull()
    {
        var result = Guard.WhenNotNull<string, int>(null, value => value.Length, -1);

        Assert.That(result, Is.EqualTo(-1));
    }
}
