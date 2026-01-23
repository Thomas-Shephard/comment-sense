using System.Collections.Immutable;
using CommentSense.Analyzers.Utilities;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Tests.Utilities;

public class AnalysisEngineTests
{
    [Test]
    public void GetPrimaryLocationReturnsLocationNoneForEmptyArray()
    {
        var locations = ImmutableArray<Location>.Empty;
        var result = AnalysisEngine.GetPrimaryLocation(locations);

        Assert.That(result, Is.EqualTo(Location.None));
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstLocation()
    {
        var location = Location.Create("test.cs", default, default);
        var locations = ImmutableArray.Create(location);
        var result = AnalysisEngine.GetPrimaryLocation(locations);

        Assert.That(result, Is.EqualTo(location));
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForImplicitlyDeclared()
    {
        const string source = "public class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ctor = symbol.GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.Constructor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctor.IsImplicitlyDeclared, Is.True);
            Assert.That(AnalysisEngine.IsEligibleForAnalysis(ctor), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForPropertyAccessors()
    {
        const string source = "public class C { public int P { get; set; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var prop = (IPropertySymbol)symbol.GetMembers().First(m => m.Name == "P");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prop.GetMethod, Is.Not.Null);
            Assert.That(prop.SetMethod, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AnalysisEngine.IsEligibleForAnalysis(prop.GetMethod), Is.False);
            Assert.That(AnalysisEngine.IsEligibleForAnalysis(prop.SetMethod), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForEventAccessors()
    {
        const string source = "using System; public class C { public event EventHandler E; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ev = (IEventSymbol)symbol.GetMembers().First(m => m.Name == "E");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ev.AddMethod, Is.Not.Null);
            Assert.That(ev.RemoveMethod, Is.Not.Null);
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(AnalysisEngine.IsEligibleForAnalysis(ev.AddMethod), Is.False);
            Assert.That(AnalysisEngine.IsEligibleForAnalysis(ev.RemoveMethod), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForInaccessibleSymbol()
    {
        const string source = "internal class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(AnalysisEngine.IsEligibleForAnalysis(method), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicSymbol()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(AnalysisEngine.IsEligibleForAnalysis(method), Is.True);
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstOfMultipleLocations()
    {
        var loc1 = Location.Create("f1.cs", default, default);
        var loc2 = Location.Create("f2.cs", default, default);
        var locations = ImmutableArray.Create(loc1, loc2);

        Assert.That(AnalysisEngine.GetPrimaryLocation(locations), Is.EqualTo(loc1));
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicProperty()
    {
        const string source = "public class C { public int P { get; set; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var prop = symbol.GetMembers().First(m => m.Name == "P");

        Assert.That(AnalysisEngine.IsEligibleForAnalysis(prop), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicField()
    {
        const string source = "public class C { public int f; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var field = symbol.GetMembers().First(m => m.Name == "f");

        Assert.That(AnalysisEngine.IsEligibleForAnalysis(field), Is.True);
    }
}
