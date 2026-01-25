using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ReturnValueDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task NonVoidMethodWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public int {|CSENSE006:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NonVoidMethodWithReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
                public int MyMethod() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task VoidMethodWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ConstructorDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public MyClass() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task TaskMethodWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public Task MyMethod() => Task.CompletedTask;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task VoidMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
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
                /// <summary>Summary</summary>
                public class Task { }
            }
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public Other.Task {|CSENSE006:MyMethod|}() => new Other.Task();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GlobalNamespaceTaskTypeReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class Task { }
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
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
                /// <summary>Summary</summary>
                public struct ValueTask { }
            }
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public Other.ValueTask {|CSENSE006:MyMethod|}() => new Other.ValueTask();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ArrayReturnTypeWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public int[] {|CSENSE006:MyMethod|}() => new int[0];
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UserDefinedOperatorWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="a">a</param>
                /// <param name="b">b</param>
                public static MyClass operator{|CSENSE006:+|}(MyClass a, MyClass b) => a;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConversionOperatorWithoutReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="a">a</param>
                public static explicit operator {|CSENSE006:int|}(MyClass a) => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns></returns>
                public int {|CSENSE006:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task AsyncVoidMethodWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <returns>Value</returns>
                public async void {|CSENSE013:MyMethod|}() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base</summary>
            public class Base
            {
                /// <summary>Base</summary>
                /// <returns>Return value</returns>
                public virtual int M() => 0;
            }

            /// <summary>Derived</summary>
            public class Derived : Base
            {
                /// <inheritdoc />
                public override int M() => 1;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IncludedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Doc</summary>
            public class MyClass
            {
                /// <include file='docs.xml' path='[@name="test"]'/>
                public int M() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
