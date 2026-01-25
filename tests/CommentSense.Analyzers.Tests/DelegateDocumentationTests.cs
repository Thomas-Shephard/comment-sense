using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class DelegateDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task DelegateMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public delegate void MyDelegate(int p1, int {|CSENSE002:p2|});
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DelegateWithDocumentedParametersDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            /// <param name="p2">p2</param>
            public delegate void MyDelegate(int p1, int p2);
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DelegateMissingTypeParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public delegate void MyDelegate<{|CSENSE004:T|}>();
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DelegateWithStrayParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            /// <param name="extra">extra</param>
            public delegate void {|CSENSE003:MyDelegate|}(int p1);
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DelegateMissingReturnValueDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public delegate int {|CSENSE006:MyDelegate|}(int p1);
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DelegateWithDocumentedReturnValueDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            /// <returns>Returns int</returns>
            public delegate int MyDelegate(int p1);
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task VoidDelegateWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public delegate void MyDelegate(int p1);
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task TaskDelegateWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>Summary</summary>
            public delegate Task MyDelegate();
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ValueTaskDelegateWithoutReturnsTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>Summary</summary>
            public delegate ValueTask MyDelegate();
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericTaskDelegateMissingReturnValueDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>Summary</summary>
            public delegate Task<int> {|CSENSE006:MyDelegate|}();
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task VoidDelegateWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <returns>Stray</returns>
            public delegate void {|CSENSE013:MyDelegate|}();
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TaskDelegateWithReturnsTagReportsDiagnostic()
    {
        const string testCode = """
            using System.Threading.Tasks;
            /// <summary>Summary</summary>
            /// <returns>Stray</returns>
            public delegate Task {|CSENSE013:MyDelegate|}();
            """;

        await VerifyCSenseAsync(testCode);
    }
}
