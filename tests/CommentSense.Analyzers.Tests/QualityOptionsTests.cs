using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class QualityOptionsTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MinSummaryLengthReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Short</summary>
                public void {|CSENSE016:Method|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "10"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task MinSummaryLengthDoesNotReportDiagnosticWhenLongEnough()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This summary is long enough.</summary>
                public void Method() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "10"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task RequireEndingPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Missing punctuation</summary>
                public void {|CSENSE016:Method|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.require_ending_punctuation"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task RequireEndingPunctuationDoesNotReportDiagnosticWhenPresent()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Has punctuation.</summary>
                public void Method() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.require_ending_punctuation"] = "true"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Calculate Total</summary>
                public void {|CSENSE016:CalculateTotal|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdDoesNotReportDiagnosticWhenDifferentEnough()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Computes the sum of all items in the current collection.</summary>
                public void CalculateTotal() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdAppliesToParameters()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                /// <param name="userId">user Id</param>
                public void Method(int {|CSENSE016:userId|}) { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdAppliesToDelegateReturns()
    {
        const string testCode = """
            /// <summary>This is a valid summary.</summary>
            /// <returns>My Callback</returns>
            public delegate int {|CSENSE016:MyCallback|}();
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdAtZeroDoesNotReport()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Calculate Total</summary>
                public void CalculateTotal() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.0"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdAtOneOnlyReportsExactMatches()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>CalculateTotal</summary>
                public void {|CSENSE016:CalculateTotal|}() { }

                /// <summary>Calculate Totals</summary>
                public void CalculateTotals() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "1.0"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task MinSummaryLengthAtZeroOrNegativeDoesNotReport()
    {
        const string testCode = """
            /// <summary>S</summary>
            public class MyClass { }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "-5"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task MultipleQualityRulesInteraction()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Short.</summary>
                public void {|CSENSE016:Method1|}() { }

                /// <summary>This is long enough but missing punctuation</summary>
                public void {|CSENSE016:Method2|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "10",
            ["comment_sense.require_ending_punctuation"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdNegativeClampsToZero()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>Calculate Total</summary>
                public void CalculateTotal() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "-0.5"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdGreaterThanOneClampsToOne()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>CalculateTotal</summary>
                public void {|CSENSE016:CalculateTotal|}() { }

                /// <summary>Calculate Totals</summary>
                public void CalculateTotals() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "1.5"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task LongDocumentationStressTest()
    {
        var longSummary = new string('a', 1000);
        var testCode = $$"""
            /// <summary>{{longSummary}}</summary>
            public class MyClass { }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.5"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }
}
