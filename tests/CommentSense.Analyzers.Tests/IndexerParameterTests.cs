using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class IndexerParameterTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task IndexerMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <value>Value at the index.</value>
                public int this[int {|CSENSE002:index|}]
                {
                    get => _arr[index];
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IndexerWithDocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="index">The index into the array.</param>
                /// <value>Value at the index.</value>
                public int this[int index]
                {
                    get => _arr[index];
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IndexerWithMultipleParametersMissingDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[,] _arr = new int[10,10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="x">The x coordinate.</param>
                /// <value>Value at the coordinates.</value>
                public int this[int x, int {|CSENSE002:y|}]
                {
                    get => _arr[x, y];
                    set => _arr[x, y] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IndexerWithStrayParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="index">The index into the array.</param>
                /// <param name="extra">An extra parameter documentation.</param>
                /// <value>Value at the index.</value>
                public int {|CSENSE003:this|}[int index]
                {
                    get => _arr[index];
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IndexerMissingReturnValueDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="index">The index into the array.</param>
                public int {|CSENSE014:this|}[int index]
                {
                    get => _arr[index];
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task IndexerWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the base class.</summary>
            public class Base
            {
                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="index">The index value.</param>
                /// <value>Value at the index.</value>
                public virtual int this[int index] => 0;
            }

            /// <inheritdoc />
            public class Derived : Base
            {
                /// <inheritdoc />
                public override int this[int index] => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task WriteOnlyIndexerDoesNotReportMissingReturnValueDocumentation()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="index">The index into the array.</param>
                public int this[int index]
                {
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
