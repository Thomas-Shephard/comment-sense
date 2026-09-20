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

    [TestCase(null, "Unexpected null value.")]
    [TestCase("Missing root.", "Missing root.")]
    public void AgainstNullThrowsForNull(string? message, string expected)
    {
        var exception = Assert.Throws<InvalidOperationException>(() => Guard.AgainstNull<string>(null, message));
        Assert.That(exception?.Message, Is.EqualTo(expected));
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
