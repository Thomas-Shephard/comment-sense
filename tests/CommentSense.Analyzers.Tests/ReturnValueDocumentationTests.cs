using System.Xml.Linq;
using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ReturnValueDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task NonVoidMethodWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public int {|CSENSE006:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NonVoidMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>Returns an integer value.</returns>
                public int MyMethod() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task VoidMethodWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ConstructorDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the constructor.</summary>
                public MyClass() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ConstructorWithValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the constructor.</summary>
                /// {|CSENSE015:<value>A stray value tag.</value>|}
                public MyClass() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrimaryConstructorWithValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The input value.</param>
            /// {|CSENSE015:<value>A stray value tag.</value>|}
            public class MyClass(int x)
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TaskMethodWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Task MyMethod() => Task.CompletedTask;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task VoidMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// {|CSENSE013:<returns>A stray returns tag.</returns>|}
                public void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TaskMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>A stray returns tag.</returns>
                public Task MyMethod() => Task.CompletedTask;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ValueTaskMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>A stray returns tag.</returns>
                public ValueTask MyMethod() => default;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericTaskMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>Returns an integer value.</returns>
                public Task<int> MyMethod() => Task.FromResult(0);
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericValueTaskMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>Returns an integer value.</returns>
                public ValueTask<int> MyMethod() => default;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericTaskMethodWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Task<int> {|CSENSE006:MyMethod|}() => Task.FromResult(0);
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueTaskMethodWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public ValueTask MyMethod() => default;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericValueTaskMethodWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public ValueTask<int> {|CSENSE006:MyMethod|}() => default;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CustomTaskTypeWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            namespace Other
            {
                /// <summary>This is a summary for the custom task class.</summary>
                public class Task { }
            }
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Other.Task {|CSENSE006:MyMethod|}() => new Other.Task();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GlobalNamespaceTaskTypeReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the custom task class.</summary>
            public class Task { }
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Task {|CSENSE006:MyMethod|}() => new Task();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CustomValueTaskTypeReportsDiagnostic()
    {
        const string testCode = """
            namespace Other
            {
                /// <summary>This is a summary for the custom value task struct.</summary>
                public struct ValueTask { }
            }
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Other.ValueTask {|CSENSE006:MyMethod|}() => new Other.ValueTask();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedTaskTypeDoesNotCrash()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public Task {|CSENSE006:MyMethod|}() => null;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: true, compilerDiagnostics: Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None);
    }

    [Test]
    public async Task ArrayReturnTypeWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public int[] {|CSENSE006:MyMethod|}() => new int[0];
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UserDefinedOperatorWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the operator.</summary>
                /// <param name="a">The first operand.</param>
                /// <param name="b">The second operand.</param>
                public static MyClass operator{|CSENSE006:+|}(MyClass a, MyClass b) => a;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConversionOperatorWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the operator.</summary>
                /// <param name="a">The operand.</param>
                public static explicit operator {|CSENSE006:int|}(MyClass a) => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// {|CSENSE016:<returns></returns>|}
                public int MyMethod() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task AsyncVoidMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// {|CSENSE013:<returns>A stray returns tag.</returns>|}
                public async void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the base class.</summary>
            public class Base
            {
                /// <summary>This is a summary for the base method.</summary>
                /// <returns>Returns an integer value.</returns>
                public virtual int M() => 0;
            }

            /// <summary>This is a summary for the derived class.</summary>
            public class Derived : Base
            {
                /// <inheritdoc />
                public override int M() => 1;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExplicitInterfaceImplementationDoesNotReportDiagnosticByDefault()
    {
        const string testCode = """
            /// <summary>This is a summary for the interface.</summary>
            public interface I
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>Returns an integer value.</returns>
                int M();
            }
            /// <summary>This is a summary for the class.</summary>
            public class C : I
            {
                /// <summary>This is the explicit implementation summary.</summary>
                int I.M() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IncludedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <include file='docs.xml' path='[@name="test"]'/>
                public int M() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StrayReturnsOnPropertyReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid property summary.</summary>
                /// <value>This is a valid value summary.</value>
                /// {|CSENSE013:<returns>Stray returns tag.</returns>|}
                public int Property { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task StrayValueOnMethodReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                /// {|CSENSE015:<value>Stray value tag.</value>|}
                public void Method() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DelegateLowQualityReturnsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid delegate summary.</summary>
            /// {|CSENSE016:<returns>DelegateName</returns>|}
            public delegate int DelegateName();
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PropertyLowQualityValueDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// {|CSENSE016:<value>MyProperty</value>|}
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PropertyLowQualityValueTypeNameDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// {|CSENSE016:<value>Int32</value>|}
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public void IsLowQualityCheckReferenceEquals()
    {
        var options = CommentSenseOptions.Default;

        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class C {}");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var symbol = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();

        var result = QualityAnalyzer.IsLowQuality(new XElement("summary", "Valid"), symbol, symbol, options);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityCheckDifferentSymbols()
    {
        var options = CommentSenseOptions.Default;

        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("class C { void M() {} }");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var type = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        var methodSymbol = type.GetMembers("M").First();

        var result = QualityAnalyzer.IsLowQuality(new XElement("summary", "Valid"), methodSymbol, type, options);
        Assert.That(result, Is.False);
    }

    [Test]
    public async Task MethodReturningDynamicDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                /// <returns>This is a valid returns summary.</returns>
                public dynamic MyMethod() => null;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MethodReturningPointerDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public unsafe class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                /// <returns>This is a valid returns summary.</returns>
                public int* MyMethod() => null;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, compilerDiagnostics: Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None);
    }

    [Test]
    public void IsNonGenericTaskNullNamespace()
    {
        var syntaxTree = Microsoft.CodeAnalysis.CSharp.CSharpSyntaxTree.ParseText("public class Task {}");
        var compilation = Microsoft.CodeAnalysis.CSharp.CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(syntaxTree);
        var symbol = compilation.GetTypeByMetadataName("Task") ?? throw new InvalidOperationException();

        var result = symbol.IsTaskType();
        Assert.That(result, Is.False);
    }
}
