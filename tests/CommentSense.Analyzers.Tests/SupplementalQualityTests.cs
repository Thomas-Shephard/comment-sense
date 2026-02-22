using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class SupplementalQualityTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task LowQualityRemarksAreFlagged()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// {|CSENSE016:<remarks>remarks</remarks>|}
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task LowQualityExampleIsFlagged()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// {|CSENSE016:<example>example</example>|}
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyRemarksAreFlagged()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// {|CSENSE016:<remarks></remarks>|}
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TodoRemarksAreFlagged()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// {|CSENSE016:<remarks>TODO</remarks>|}
            public class MyClass { }
            """;

        var options = new Dictionary<string, string>
        {
            { "comment_sense.low_quality_terms", "TODO" }
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task ExampleWithCodeIsNoLowQuality()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// <example>
            /// <code>
            /// var x = new MyClass();
            /// </code>
            /// </example>
            public class MyClass { }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
