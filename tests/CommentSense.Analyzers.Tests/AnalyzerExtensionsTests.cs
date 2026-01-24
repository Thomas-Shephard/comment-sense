using System.Collections.Immutable;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class AnalyzerExtensionsTests
{
    [Test]
    public void GetPrimaryLocationReturnsLocationNoneForEmptyArray()
    {
        var locations = ImmutableArray<Location>.Empty;
        var result = locations.GetPrimaryLocation();

        Assert.That(result, Is.EqualTo(Location.None));
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstLocation()
    {
        var location = Location.Create("test.cs", default, default);
        var locations = ImmutableArray.Create(location);
        var result = locations.GetPrimaryLocation();

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
            Assert.That(ctor.IsEligibleForAnalysis(), Is.False);
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
            Assert.That(prop.GetMethod?.IsEligibleForAnalysis(), Is.False);
            Assert.That(prop.SetMethod?.IsEligibleForAnalysis(), Is.False);
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
            Assert.That(ev.AddMethod?.IsEligibleForAnalysis(), Is.False);
            Assert.That(ev.RemoveMethod?.IsEligibleForAnalysis(), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForInaccessibleSymbol()
    {
        const string source = "internal class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicSymbol()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstOfMultipleLocations()
    {
        var loc1 = Location.Create("f1.cs", default, default);
        var loc2 = Location.Create("f2.cs", default, default);
        var locations = ImmutableArray.Create(loc1, loc2);

        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(loc1));
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicProperty()
    {
        const string source = "public class C { public int P { get; set; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var prop = symbol.GetMembers().First(m => m.Name == "P");

        Assert.That(prop.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicField()
    {
        const string source = "public class C { public int f; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var field = symbol.GetMembers().First(m => m.Name == "f");

        Assert.That(field.IsEligibleForAnalysis(), Is.True);
    }
}
