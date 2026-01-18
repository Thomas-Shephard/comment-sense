using Microsoft.CodeAnalysis;
using CommentSense.Utilities;
using NUnit.Framework;

namespace CommentSense.Tests.Utilities;

public class AccessibilityExtensionsTests
{
    [TestCase("public class C { public void M() { int x = 0; } }", "x", false)]
    [TestCase("public class C { public void M() {} }", "M", true)]
    [TestCase("internal class C { public void M() {} }", "M", false)]
    [TestCase("public class C { private void M() {} }", "M", false)]
    [TestCase("public class C { protected void M() {} }", "M", true)]
    [TestCase("public class C { internal void M() {} }", "M", false)]
    [TestCase("public class C { protected internal void M() {} }", "M", true)]
    [TestCase("public class C { private protected void M() {} }", "M", false)]
    [TestCase("public class Outer { internal class Inner { public void M() {} } }", "M", false)]
    [TestCase("public class Outer { public class Inner { public void M() {} } }", "M", true)]
    public void IsEffectivelyAccessibleReturnsExpectedValue(string source, string symbolName, bool expected)
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(source, symbolName);
        Assert.That(symbol.IsEffectivelyAccessible(), Is.EqualTo(expected));
    }

    [Test]
    public void IsEffectivelyAccessibleReturnsFalseForNull()
    {
        Assert.That(AccessibilityExtensions.IsEffectivelyAccessible(null), Is.False);
    }
}
