using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class EnumAnalysisTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task EnumMemberWithoutDocumentationReportsDiagnosticByDefault()
    {
        const string testCode = """
            /// <summary>This is a summary for the enum.</summary>
            public enum MyEnum
            {
                {|CSENSE001:Value1|},
                /// <summary>This is a summary for Value2.</summary>
                Value2
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EnumMemberWithoutDocumentationDoesNotReportDiagnosticWhenExcluded()
    {
        const string testCode = """
            /// <summary>This is a summary for the enum.</summary>
            public enum MyEnum
            {
                Value1,
                /// <summary>This is a summary for Value2.</summary>
                Value2
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_enums"] = "true"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task EnumMemberWithoutDocumentationReportsDiagnosticWhenExcludedIsFalse()
    {
        const string testCode = """
            /// <summary>This is a summary for the enum.</summary>
            public enum MyEnum
            {
                {|CSENSE001:Value1|}
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_enums"] = "false"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task EnumDeclarationStillRequiresDocumentationWhenMembersAreExcluded()
    {
        const string testCode = """
            public enum {|CSENSE001:MyEnum|}
            {
                Value1,
                Value2
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_enums"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }
}
