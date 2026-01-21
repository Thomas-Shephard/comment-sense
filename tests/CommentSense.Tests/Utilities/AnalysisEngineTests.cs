using System.Collections.Immutable;
using CommentSense.Utilities;
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
    public void ShouldReportDiagnosticReturnsFalseForImplicitlyDeclared()
    {
        const string source = "public class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ctor = symbol.GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.Constructor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctor.IsImplicitlyDeclared, Is.True);
            Assert.That(AnalysisEngine.ShouldReportDiagnostic(ctor), Is.False);
        }
    }

    [Test]
    public void ShouldReportDiagnosticReturnsFalseForPropertyAccessors()
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
            Assert.That(AnalysisEngine.ShouldReportDiagnostic(prop.GetMethod), Is.False);
            Assert.That(AnalysisEngine.ShouldReportDiagnostic(prop.SetMethod), Is.False);
        }
    }

    [Test]
    public void ShouldReportDiagnosticReturnsFalseForEventAccessors()
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
            Assert.That(AnalysisEngine.ShouldReportDiagnostic(ev.AddMethod), Is.False);
            Assert.That(AnalysisEngine.ShouldReportDiagnostic(ev.RemoveMethod), Is.False);
        }
    }

    [Test]
    public void ShouldReportDiagnosticReturnsFalseForInaccessibleSymbol()
    {
        const string source = "internal class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(AnalysisEngine.ShouldReportDiagnostic(method), Is.False);
    }

    [Test]
    public void ShouldReportDiagnosticReturnsFalseForDocumentedSymbol()
    {
        const string source = """
            public class C {
            /// <summary>Docs</summary>
                public void M() {}
            }
            """;
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C", parseDocumentation: true);
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(AnalysisEngine.ShouldReportDiagnostic(method), Is.False);
    }

    [Test]
    public void ShouldReportDiagnosticReturnsTrueForPublicUndocumentedSymbol()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(AnalysisEngine.ShouldReportDiagnostic(method), Is.True);
    }
}
