using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

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
    [TestCase("public class C { public void M() { label: goto label; } }", "label", false)]
    [TestCase("using System.Linq; public class C { public void M() { var list = new int[0]; var query = from x in list select x; } }", "x", false)]
    [TestCase("public class C { static C() {} }", "C", true)]
    public void IsEffectivelyAccessibleReturnsExpectedValue(string source, string symbolName, bool expected)
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(source, symbolName);
        Assert.That(symbol.IsEffectivelyAccessible(), Is.EqualTo(expected));
    }

    [TestCase("public class C { public void M() {} }", "M", VisibilityLevel.Public, true)]
    [TestCase("public class C { protected void M() {} }", "M", VisibilityLevel.Public, false)]
    [TestCase("public class C { internal void M() {} }", "M", VisibilityLevel.Internal, true)]
    [TestCase("public class C { private void M() {} }", "M", VisibilityLevel.Internal, false)]
    [TestCase("public class C { private void M() {} }", "M", VisibilityLevel.Private, true)]
    [TestCase("internal class C { public void M() {} }", "M", VisibilityLevel.Internal, true)]
    [TestCase("internal class C { public void M() {} }", "M", VisibilityLevel.Public, false)]
    public void IsEffectivelyAccessibleWithVisibilityLevel(string source, string symbolName, VisibilityLevel level, bool expected)
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(source, symbolName);
        Assert.That(symbol.IsEffectivelyAccessible(level), Is.EqualTo(expected));
    }

    [Test]
    public void IsEffectivelyAccessibleWithInvalidVisibilityLevelReturnsFalse()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource("public class C {}", "C");
        Assert.That(symbol.IsEffectivelyAccessible((VisibilityLevel)999), Is.False);
    }

    [Test]
    public void IsEffectivelyAccessibleReturnsTrueForAssembly()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource("public class C {}", "C");
        var assembly = symbol.ContainingAssembly;
        Assert.That(assembly.IsEffectivelyAccessible(), Is.True);
    }

    [Test]
    public void IsEffectivelyAccessibleReturnsFalseForNull()
    {
        Assert.That(AccessibilityExtensions.IsEffectivelyAccessible(null), Is.False);
    }
}
