using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class SymbolExtensionsTests
{
    [Test]
    public void GetParametersMethodSymbolReturnsParameters()
    {
        var symbol = GetSymbol("public class C { void M(int p1) {} }", "M");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetParametersPropertySymbolReturnsIndexerParameters()
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
    public void GetParametersDelegateTypeReturnsInvokeParameters()
    {
        var symbol = GetSymbol("public delegate void D(int p1);", "D");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetParametersRecordWithPrimaryConstructorReturnsParameters()
    {
        var symbol = GetSymbol("public record R(int p1);", "R");
        var parameters = symbol.GetParameters();
        Assert.That(parameters, Has.Length.EqualTo(1));
        Assert.That(parameters[0].Name, Is.EqualTo("p1"));
    }

    [Test]
    public void GetParametersFieldSymbolReturnsEmpty()
    {
        var symbol = GetSymbol("public class C { int F; }", "F");
        var parameters = symbol.GetParameters();
        Assert.That(parameters.IsEmpty, Is.True);
    }

    [Test]
    public void GetParametersNormalClassSymbolReturnsEmpty()
    {
        var symbol = GetSymbol("public class C { public C() {} }", "C");
        var parameters = symbol.GetParameters();
        Assert.That(parameters.IsEmpty, Is.True);
    }

    [Test]
    public void GetTypeParametersMethodSymbolReturnsTypeParameters()
    {
        var symbol = GetSymbol("public class C { void M<T>() {} }", "M");
        var typeParams = symbol.GetTypeParameters();
        Assert.That(typeParams, Has.Length.EqualTo(1));
        Assert.That(typeParams[0].Name, Is.EqualTo("T"));
    }

    [Test]
    public void GetTypeParametersNamedTypeSymbolReturnsTypeParameters()
    {
        var symbol = GetSymbol("public class C<T> {}", "C");
        var typeParams = symbol.GetTypeParameters();
        Assert.That(typeParams, Has.Length.EqualTo(1));
        Assert.That(typeParams[0].Name, Is.EqualTo("T"));
    }

    [Test]
    public void GetTypeParametersFieldSymbolReturnsEmpty()
    {
        var symbol = GetSymbol("public class C { int F; }", "F");
        var typeParams = symbol.GetTypeParameters();
        Assert.That(typeParams.IsEmpty, Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsSameClassReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("class C {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var symbol = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(symbol.InheritsFromOrEquals(symbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsBaseClassReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("class B {} class C : B {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var b = compilation.GetTypeByMetadataName("B") ?? throw new InvalidOperationException();
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(c.InheritsFromOrEquals(b), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsInterfaceReturnsTrue()
    {
        var tree = CSharpSyntaxTree.ParseText("interface I {} class C : I {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var i = compilation.GetTypeByMetadataName("I") ?? throw new InvalidOperationException();
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(c.InheritsFromOrEquals(i), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsUnrelatedClassReturnsFalse()
    {
        var tree = CSharpSyntaxTree.ParseText("class A {} class B {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var a = compilation.GetTypeByMetadataName("A") ?? throw new InvalidOperationException();
        var b = compilation.GetTypeByMetadataName("B") ?? throw new InvalidOperationException();
        Assert.That(a.InheritsFromOrEquals(b), Is.False);
    }

    [Test]
    public void InheritsFromOrEqualsUnimplementedInterfaceReturnsFalse()
    {
        var tree = CSharpSyntaxTree.ParseText("interface I {} class C {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var i = compilation.GetTypeByMetadataName("I") ?? throw new InvalidOperationException();
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        Assert.That(c.InheritsFromOrEquals(i), Is.False);
    }

    [Test]
    public void InheritsFromOrEqualsObjectBaseReturnsCorrectResults()
    {
        var tree = CSharpSyntaxTree.ParseText("class C {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var c = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        var obj = compilation.GetSpecialType(SpecialType.System_Object);
        using (Assert.EnterMultipleScope())
        {
            // C inherits from Object.
            Assert.That(c.InheritsFromOrEquals(obj), Is.True);

            // Object inherits from nothing.
            Assert.That(obj.InheritsFromOrEquals(c), Is.False);
        }
    }

    [Test]
    public void GetAssociatedSymbolVariableDeclaratorReturnsFieldSymbol()
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
    public void GetAssociatedSymbolVariableDeclaratorReturnsLocalSymbol()
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
    public void GetAssociatedSymbolMethodDeclarationReturnsMethodSymbol()
    {
        var source = "class C { void M() {} }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var node = root.DescendantNodes().OfType<MethodDeclarationSyntax>().First();

        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Name, Is.EqualTo("M"));
    }

    [Test]
    public void GetAssociatedSymbolFieldDeclarationReturnsFirstFieldSymbol()
    {
        var source = "class C { int f1; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var node = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();

        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Name, Is.EqualTo("f1"));
    }

    [Test]
    public void GetAssociatedSymbolNodeOutsideVariablesReturnsFirstField()
    {
        var source = "class C { int f1, f2; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var fieldDecl = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();

        var node = fieldDecl.Declaration.Type;

        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Not.Null);
        Assert.That(symbol.Name, Is.EqualTo("f1"));
    }

    [Test]
    public void GetAssociatedSymbolNodeInSpecificVariableReturnsThatVariable()
    {
        var source = "class C { int f1, f2 = 0; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var v2 = root.DescendantNodes().OfType<VariableDeclaratorSyntax>().First(v => v.Identifier.ValueText == "f2");

        var node = v2.Initializer?.Value ?? throw new InvalidOperationException();
        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol?.Name, Is.EqualTo("f2"));
    }

    [Test]
    public void GetAssociatedSymbolDetachedNodeReturnsNull()
    {
        var node = SyntaxFactory.IdentifierName("test");
        var tree = CSharpSyntaxTree.ParseText("");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var symbol = node.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Null);
    }

    [Test]
    public void GetAssociatedSymbolNullMemberReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("");
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(tree);
        var node = tree.GetRoot();
        Assert.That(node.GetAssociatedSymbol(compilation.GetSemanticModel(tree)), Is.Null);
    }

    [Test]
    public void GetAssociatedSymbolEmptyFieldVariablesReturnsNull()
    {
        var fieldDecl = SyntaxFactory.FieldDeclaration(
            SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))));

        var tree = CSharpSyntaxTree.ParseText("");
        var compilation = CSharpCompilation.Create("Test").AddSyntaxTrees(tree);
        var symbol = fieldDecl.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol, Is.Null);
    }

    [Test]
    public void GetAssociatedSymbolNodeInFieldButNotVariableReturnsFirstVariable()
    {
        var source = "class C { int f1, f2; }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var root = tree.GetRoot();
        var fieldDecl = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();

        // Pass the field declaration itself. Span won't be contained in any variable declarator span.
        var symbol = fieldDecl.GetAssociatedSymbol(compilation.GetSemanticModel(tree));
        Assert.That(symbol?.Name, Is.EqualTo("f1"));
    }

    [Test]
    public void GetPrimaryConstructorNonClassOrStructReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("enum E { A }");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var type = compilation.GetTypeByMetadataName("E") ?? throw new InvalidOperationException();
        Assert.That(type.GetPrimaryConstructor(), Is.Null);
    }

    [Test]
    public void GetPrimaryConstructorStructWithPrimaryReturnsConstructor()
    {
        var source = "public struct S(int x) {}";
        var symbol = (INamedTypeSymbol)GetSymbol(source, "S");
        Assert.That(symbol.GetPrimaryConstructor(), Is.Not.Null);
    }

    [Test]
    public void IsPrimaryConstructorNormalConstructorReturnsFalse()
    {
        var source = "public class C { public C() {} }";
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var type = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        var ctor = type.InstanceConstructors.First();

        Assert.That(ctor.IsPrimaryConstructor(), Is.False);
    }

    [Test]
    public void IsPrimaryConstructorMethodSymbolReturnsFalse()
    {
        var symbol = (IMethodSymbol)GetSymbol("public class C { void M() {} }", "M");
        Assert.That(symbol.IsPrimaryConstructor(), Is.False);
    }

    private static ISymbol GetSymbol(string source, string name)
        => RoslynTestUtils.GetSymbolFromSource(source, name);
}
