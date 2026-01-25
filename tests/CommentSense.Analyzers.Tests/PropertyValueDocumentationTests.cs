using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class PropertyValueDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task PropertyMissingValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                public int {|CSENSE014:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task PropertyWithValueTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the property.</value>
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ReadOnlyPropertyMissingValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                public int {|CSENSE014:MyProperty|} => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task WriteOnlyPropertyDoesNotRequireValueTag()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>This is a summary for the property.</summary>
                public int MyProperty { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropertyWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the property.</value>
                /// <returns>Value of the property.</returns>
                public int {|CSENSE013:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithValueTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <value>Value of the method.</value>
                /// <returns>Value of the method.</returns>
                public int {|CSENSE015:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task WriteOnlyPropertyWithValueTagDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the property.</value>
                public int MyProperty { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IndexerWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="i">The index value.</param>
                /// <value>Value of the indexer.</value>
                /// <returns>Value of the indexer.</returns>
                public int {|CSENSE013:this|}[int i] => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PropertyWithEmptyValueTagReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value></value>
                public int {|CSENSE016:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task PropertyValueRepeatingPropertyNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>MyProperty</value>
                public int {|CSENSE016:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task WriteOnlyPropertyWithReturnsTagReportsStrayDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int _val;
                /// <summary>This is a summary for the property.</summary>
                /// <returns>Stray return documentation.</returns>
                public int {|CSENSE013:MyProperty|} { set => _val = value; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
