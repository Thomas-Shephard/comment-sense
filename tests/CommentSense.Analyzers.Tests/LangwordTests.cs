using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class LangwordTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task SummaryWithTrueReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Returns {|CSENSE019:true|} if successful.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithFalseReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Returns {|CSENSE019:false|} if failed.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithNullReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Returns {|CSENSE019:null|} if not found.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithVoidReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This method returns {|CSENSE019:void|}.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithCapitalizedTrueReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>{|CSENSE019:True|} if successful.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task WordInsideOtherWordDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a trustworthy and avoidant class.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MultiLineSummaryWithKeywordsReportsDiagnostics()
    {
        const string testCode = """
            /// <summary>
            /// This is {|CSENSE019:true|} on line 1.
            /// And {|CSENSE019:false|} on line 2.
            /// </summary>
            public class MyClass { }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EscapedCharactersInSummaryAlignCorrectly()
    {
        const string testCode = """
            /// <summary>Value &lt;{|CSENSE019:void|}&gt; is used.</summary>
            public class MyClass { }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task KeywordsInCodeBlocksDoNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>
            /// <code>
            /// var x = true;
            /// </code>
            /// <c>null</c>
            /// </summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MultipleKeywordsReportMultipleDiagnostics()
    {
        const string testCode = """
            /// <summary>Returns {|CSENSE019:true|} or {|CSENSE019:false|} or {|CSENSE019:null|}.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConfigurationWorks()
    {
        const string testCode = """
            /// <summary>This is a {|CSENSE019:class|} with {|CSENSE019:while|} loop.</summary>
            public class MyClass
            {
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.langwords", "class,while" }
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task EmptyLangwordsConfigDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This true false null void should not be flagged.</summary>
            public class MyClass
            {
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.langwords", "" }
        };

        await VerifyCSenseAsync(testCode, configOptions: config, expectDiagnostic: false);
    }
}
