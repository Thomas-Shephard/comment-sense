using NUnit.Framework;

namespace CommentSense.Tests;

public class PlaceholderTests
{
    [Test]
    public void ShouldBeAlive()
    {
        Assert.That(Placeholder.IsAlive(), Is.True);
    }
}
