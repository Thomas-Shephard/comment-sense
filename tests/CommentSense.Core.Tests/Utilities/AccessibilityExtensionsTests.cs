using Microsoft.CodeAnalysis;
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
    public void IsEffectivelyAccessibleVariousSymbolsReturnsExpected(string source, string symbolName, bool expected)
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
    public void IsEffectivelyAccessibleWithVisibilityLevelReturnsExpected(string source, string symbolName, VisibilityLevel level, bool expected)
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource(source, symbolName);
        Assert.That(symbol.IsEffectivelyAccessible(level), Is.EqualTo(expected));
    }

    [Test]
    public void IsEffectivelyAccessibleGenericWithInternalArgumentReturnsExpected()
    {
        const string source = "using System.Collections.Generic; internal class InternalType {} public class C { internal List<InternalType> f; }";
        var symbol = (IFieldSymbol)RoslynTestUtils.GetSymbolFromSource(source, "f");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(symbol.IsEffectivelyAccessible(), Is.False);
            Assert.That(symbol.IsEffectivelyAccessible(VisibilityLevel.Internal), Is.True);
        }
    }
    [Test]
    public void GetEffectiveVisibilityLevelGenericWithInternalArgumentReturnsInternal()
    {
        const string source = "using System.Collections.Generic; internal class InternalType {} public class C { private List<InternalType> f; }";
        var symbol = (IFieldSymbol)RoslynTestUtils.GetSymbolFromSource(source, "f");
        var type = symbol.Type;

        Assert.That(type.GetEffectiveVisibilityLevel(), Is.EqualTo(VisibilityLevel.Internal));
    }

    [Test]
    public void IsEffectivelyAccessibleInvalidVisibilityLevelReturnsFalse()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource("public class C {}", "C");
        Assert.That(symbol.IsEffectivelyAccessible((VisibilityLevel)999), Is.False);
    }

    [Test]
    public void IsEffectivelyAccessibleAssemblySymbolReturnsTrue()
    {
        var symbol = RoslynTestUtils.GetSymbolFromSource("public class C {}", "C");
        var assembly = symbol.ContainingAssembly;
        Assert.That(assembly.IsEffectivelyAccessible(), Is.True);
    }

    [Test]
    public void IsEffectivelyAccessibleNullSymbolReturnsFalse()
    {
        Assert.That(AccessibilityExtensions.IsEffectivelyAccessible(null), Is.False);
    }
}
