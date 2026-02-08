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

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForSameClass()
    {
        var tree = CSharpSyntaxTree.ParseText("class C {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var symbol = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(symbol.InheritsFromOrEquals(symbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForBaseClass()
    {
        var tree = CSharpSyntaxTree.ParseText("class B {} class C : B {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var b = compilation.GetTypeByMetadataName("B") ?? throw new InvalidOperationException();
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(c.InheritsFromOrEquals(b), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForInterface()
    {
        var tree = CSharpSyntaxTree.ParseText("interface I {} class C : I {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var i = compilation.GetTypeByMetadataName("I") ?? throw new InvalidOperationException();
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(c.InheritsFromOrEquals(i), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsFalseForUnrelated()
    {
        var tree = CSharpSyntaxTree.ParseText("class A {} class B {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var a = compilation.GetTypeByMetadataName("A") ?? throw new InvalidOperationException();
        var b = compilation.GetTypeByMetadataName("B") ?? throw new InvalidOperationException();
        Assert.That(a.InheritsFromOrEquals(b), Is.False);
    }

    [Test]
    public void GetAssociatedSymbolReturnsSymbolForField()
    {
        var source = "class C { int f1, f2; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var node = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(v => v.Identifier.ValueText == "f1");

        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Name, Is.EqualTo("f1"));
    }

    [Test]
    public void GetAssociatedSymbolReturnsSymbolForLocal()
    {
        var source = "class C { void M() { int l1 = 0; } }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var node = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(v => v.Identifier.ValueText == "l1");

        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Name, Is.EqualTo("l1"));
    }

    [Test]
    public void GetAssociatedSymbolReturnsNullForDetachedNode()
    {
        var node = SyntaxFactory.IdentifierName("test");
        var tree = CSharpSyntaxTree.ParseText("");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Null);
    }

    [Test]
    public void GetPrimaryConstructorReturnsNullForEnum()
    {
        var tree = CSharpSyntaxTree.ParseText("enum E { A }");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var type = compilation.GetTypeByMetadataName("E") ?? throw new InvalidOperationException();
        Assert.That(type.GetPrimaryConstructor(), Is.Null);
    }
}
