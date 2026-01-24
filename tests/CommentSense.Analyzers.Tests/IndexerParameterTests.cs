using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class IndexerParameterTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task IndexerMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>Summary</summary>
                /// <returns>The value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>Summary</summary>
                /// <param name="index">The index</param>
                /// <returns>The value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[,] _arr = new int[10,10];

                /// <summary>Summary</summary>
                /// <param name="x">X</param>
                /// <returns>The value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>Summary</summary>
                /// <param name="index">The index</param>
                /// <param name="extra">Extra param</param>
                /// <returns>The value</returns>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>Summary</summary>
                /// <param name="index">Index</param>
                public int {|CSENSE006:this|}[int index]
                {
                    get => _arr[index];
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IndexerWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Summary</summary>
                /// <param name="index">Index</param>
                /// <returns>The value</returns>
                public virtual int this[int index] => 0;
            }

            /// <summary>Derived class</summary>
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
            /// <summary>Summary</summary>
            public class MyClass
            {
                private int[] _arr = new int[10];

                /// <summary>Summary</summary>
                /// <param name="index">Index</param>
                public int this[int index]
                {
                    set => _arr[index] = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
