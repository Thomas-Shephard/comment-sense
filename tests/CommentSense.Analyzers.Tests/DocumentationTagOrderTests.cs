using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class DocumentationTagOrderTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    private static readonly (string Id, ReportDiagnostic Severity)[] SuppressAll =
    [
        (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.MissingReturnValueDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.MissingValueDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
    ];

    [Test]
    public async Task InheritDocShouldBeFirst()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            /// {|CSENSE024:<inheritdoc />|}
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task SummaryShouldBeBeforeParam()
    {
        const string testCode = """
            /// <param name="p">Param.</param>
            /// {|CSENSE024:<summary>Documentation.</summary>|}
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task ParamShouldBeBeforeReturns()
    {
        const string testCode = """
            /// <returns>Returns.</returns>
            /// {|CSENSE024:<param name="p">Param.</param>|}
            public class MyClass
            {
                public int MyMethod(int p) => p;
            }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task ReturnsShouldBeBeforeException()
    {
        const string testCode = """
            /// <exception cref="System.Exception">Ex.</exception>
            /// {|CSENSE024:<returns>Returns.</returns>|}
            public class MyClass
            {
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task ExceptionShouldBeBeforeRemarks()
    {
        const string testCode = """
            /// <remarks>Remarks.</remarks>
            /// {|CSENSE024:<exception cref="System.Exception">Ex.</exception>|}
            public class MyClass
            {
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task CorrectOrderReportsNoDiagnostics()
    {
        const string testCode = """
            /// <inheritdoc />
            /// <summary>Documentation.</summary>
            /// <typeparam name="T">Type.</typeparam>
            /// <param name="p">Param.</param>
            /// <returns>Returns.</returns>
            /// <exception cref="System.Exception">Ex.</exception>
            /// <remarks>Remarks.</remarks>
            public class MyClass
            {
                public int MyMethod<T>(int p) => p;
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task ValueIsSamePriorityAsReturns()
    {
        const string testCode = """
            /// <summary>Documentation.</summary>
            /// <value>Value.</value>
            /// <exception cref="System.Exception">Ex.</exception>
            public class MyClass
            {
                public int MyProperty { get; set; }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: SuppressAll);
    }

    [Test]
    public async Task CustomTagOrderIsRespected()
    {
        const string testCode = """
            /// <summary>Documentation.</summary>
            /// <remarks>Remarks.</remarks>
            /// {|CSENSE024:<param name="p">Documentation.</param>|}
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.tag_order", "summary, param, remarks" }
        };

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll, configOptions: config);
    }

    [Test]
    public async Task MultipleOutOrderSameTagsTriggerOccurrence()
    {
        const string testCode = """
            /// <remarks>R.</remarks>
            /// {|CSENSE024:<summary>S1.</summary>|}
            /// {|CSENSE022:<summary>S2.</summary>|}
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressAll);
    }
}
