using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class SymbolExtensionsTests
{
    private static ISymbol GetSymbol(string source, string name)
        => RoslynTestUtils.GetSymbolFromSource(source, name);

    [Test]
    public void GetParametersReturnsMethodParameters()
    {
        var symbol = GetSymbol("public class C { void M(int p1) {} }", "M");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetParametersReturnsPropertyParameters()
    {
        var source = "public class C { public int this[int index] => 0; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(tree);
        var root = tree.GetRoot();
        var indexer = root.DescendantNodes().OfType<IndexerDeclarationSyntax>().First();
        var symbol = compilation.GetSemanticModel(tree).GetDeclaredSymbol(indexer) ?? throw new InvalidOperationException();

        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("index"));
    }

    [Test]
    public void GetParametersReturnsDelegateParameters()
    {
        var symbol = GetSymbol("public delegate void D(int p1);", "D");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetParametersReturnsPrimaryConstructorParameters()
    {
        var symbol = GetSymbol("public record R(int p1);", "R");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetTypeParametersReturnsMethodTypeParameters()
    {
        var symbol = GetSymbol("public class C { void M<T>() {} }", "M");
        var typeParams = symbol.GetTypeParameters();
        Assert.That(typeParams, Has.Length.EqualTo(1));
        Assert.That(typeParams[0].Name, Is.EqualTo("T"));
    }

    [Test]
    public void GetTypeParametersReturnsTypeTypeParameters()
    {
        var symbol = GetSymbol("public class C<T> {}", "C");
        var typeParams = symbol.GetTypeParameters();
        Assert.That(typeParams, Has.Length.EqualTo(1));
        Assert.That(typeParams[0].Name, Is.EqualTo("T"));
    }

    [Test]
    public void GetParametersReturnsEmptyForField()
    {
        var symbol = GetSymbol("public class C { int F; }", "F");
        var parameters = symbol.GetParameters();
        Assert.That(parameters.IsEmpty, Is.True);
    }
}
