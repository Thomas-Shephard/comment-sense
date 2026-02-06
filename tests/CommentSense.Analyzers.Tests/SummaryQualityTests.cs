using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class SummaryQualityTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task SummaryRepeatingMethodNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>Save</summary>|}
                public void Save() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryRepeatingClassNameReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SelfClosingSummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary />|}
                /// <param name="x">The param x.</param>
                public void MyMethod(int x) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptySummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary></summary>|}
                /// <param name="x">The param x.</param>
                public void MyMethod(int x) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task WhitespaceSummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>   </summary>|}
                /// <param name="x">The param x.</param>
                public void MyMethod(int x) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GoodSummaryDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Saves the current state to the database.</summary>
                public void Save() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SummaryRepeatingMethodNameCaseInsensitiveReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>save</summary>|}
                public void Save() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithOnlySummaryTextReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>summary</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithOnlySummaryTextAndDotReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>Summary.</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithMultipleTrailingPeriodsReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass..</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryRepeatingIndexerNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>this[]</summary>|}
                /// <param name="i">The index.</param>
                /// <value>The value.</value>
                public int this[int i] => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithOnlyPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>...</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithTrailingExclamationReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass!</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithTrailingQuestionMarkReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass?</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithTrailingColonReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass:</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithMixedTrailingPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass! ? : ..</summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithTrailingSpaceAfterPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>MyClass. </summary>|}
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryWithChildElementsIsNotLowQuality()
    {
        const string testCode = """
            using System;
            /// <summary><see cref="String"/></summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FieldSummaryIsAnalyzed()
    {
        // Field "F" with summary "F" should be low quality
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>F</summary>|}
                public int F;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EventSummaryIsAnalyzed()
    {
        // Event "E" with summary "E" should be low quality
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>E</summary>|}
                public event EventHandler E;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EventWithAccessorsSummaryIsAnalyzed()
    {
        // Event with accessors
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>E</summary>|}
                public event EventHandler E
                {
                    add { }
                    remove { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CustomLowQualityTermWithPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>TODO.</summary>|}
            public class MyClass
            {
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.low_quality_terms"] = "TODO"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SummaryRepeatingTagKeywordWithCustomTermsReportsDiagnostic()
    {
        const string testCode = """
            /// {|CSENSE016:<summary>summary</summary>|}
            public class MyClass
            {
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.low_quality_terms"] = "TBD"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdWithPunctuationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>Calculate Total.</summary>|}
                public void CalculateTotal() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task SimilarityThresholdCaseInsensitiveReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>CALCULATE TOTAL</summary>|}
                public void CalculateTotal() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.similarity_threshold"] = "0.8"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }
}
