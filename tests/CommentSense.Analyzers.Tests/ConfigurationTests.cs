using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ConfigurationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task CustomLowQualityTermsReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>TODO</summary>
                public void {|CSENSE016:Method|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.low_quality_terms"] = "TODO, TBD"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task IgnoredExceptionsDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                public void Method()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.ignored_exceptions"] = "System.ArgumentNullException"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task IgnoredExceptionsByNameDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                public void Method()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.ignored_exceptions"] = "ArgumentNullException"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task InternalMemberIgnoredByDefault()
    {
        const string testCode = """
            internal class MyClass
            {
                public void Method() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InternalMemberAnalyzedWhenOptionEnabled()
    {
        const string testCode = """
            internal class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:Method|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.analyze_internal"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task PrivateProtectedMemberIgnoredByDefault()
    {
        const string testCode = """
            /// <summary>Valid.</summary>
            public class MyClass
            {
                private protected void Method() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PrivateProtectedMemberAnalyzedWhenOptionEnabled()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                private protected void {|CSENSE001:Method|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.analyze_internal"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task ProtectedInternalMemberAnalyzedByDefault()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                protected internal void {|CSENSE001:Method|}() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
