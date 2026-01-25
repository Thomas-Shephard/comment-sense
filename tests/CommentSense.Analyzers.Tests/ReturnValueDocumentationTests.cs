using CommentSense.TestHelpers;
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
                /// <value>A stray value tag.</value>
                public {|CSENSE015:MyClass|}() { }
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
            /// <value>A stray value tag.</value>
            public class {|CSENSE015:MyClass|}(int x)
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
                /// <returns>A stray returns tag.</returns>
                public void {|CSENSE013:MyMethod|}() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TaskMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>A stray returns tag.</returns>
                public Task {|CSENSE013:MyMethod|}() => Task.CompletedTask;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueTaskMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>A stray returns tag.</returns>
                public ValueTask {|CSENSE013:MyMethod|}() => default;
            }
            """;

        await VerifyCSenseAsync(testCode);
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
                /// <returns></returns>
                public int {|CSENSE016:MyMethod|}() => 0;
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
                /// <returns>A stray returns tag.</returns>
                public async void {|CSENSE013:MyMethod|}() { }
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
}
