using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ParameterDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public void MyMethod(int {|CSENSE002:param1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="param1">Param 1</param>
                public void MyMethod(int param1) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StrayParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="param1">Param 1</param>
                /// <param name="param2">Param 2</param>
                public void {|CSENSE003:MyMethod|}(int param1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithNoParametersDoesNotReportParameterDiagnostics()
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
    public async Task MultipleMissingParameterDocumentationsReportDiagnostics()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public void MyMethod(int {|CSENSE002:p1|}, string {|CSENSE002:p2|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConstructorWithMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public MyClass(int {|CSENSE002:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PartiallyMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="p1">p1</param>
                public void MyMethod(int p1, int {|CSENSE002:p2|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="p1"></param>
                public void MyMethod(int {|CSENSE002:p1|}) { }
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
                /// <param name="p">P</param>
                public virtual void M(int p) { }
            }

            /// <summary>Derived</summary>
            public class Derived : Base
            {
                /// <inheritdoc />
                public override void M(int p) { }
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
                public void M(int p) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ParameterOrderMismatchReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="p2">p2</param>
                /// <param name="p1">p1</param>
                public void {|CSENSE008:MyMethod|}(int p1, int p2) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="p1">p1</param>
                /// <param name="p1">p1 duplicate</param>
                public void {|CSENSE009:MyMethod|}(int p1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CorrectParameterOrderDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="p1">p1</param>
                /// <param name="p2">p2</param>
                public void MyMethod(int p1, int p2) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
