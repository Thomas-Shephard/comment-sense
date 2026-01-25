using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class PropertyValueDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task PropertyMissingValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public int {|CSENSE014:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task PropertyWithValueTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <value>The value</value>
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ReadOnlyPropertyMissingValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public int {|CSENSE014:MyProperty|} => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task WriteOnlyPropertyDoesNotRequireValueTag()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>Summary</summary>
                public int MyProperty { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropertyWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <value>Value</value>
                /// <returns>Value</returns>
                public int {|CSENSE013:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithValueTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <value>Value</value>
                /// <returns>Value</returns>
                public int {|CSENSE015:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task WriteOnlyPropertyWithValueTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>Summary</summary>
                /// <value>Value</value>
                public int MyProperty { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IndexerWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <param name="i">index</param>
                /// <value>Value</value>
                /// <returns>Value</returns>
                public int {|CSENSE013:this|}[int i] => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PropertyWithEmptyValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <value></value>
                public int {|CSENSE014:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task WriteOnlyPropertyWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>Summary</summary>
                /// <returns>Stray</returns>
                public int {|CSENSE013:MyProperty|} { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
