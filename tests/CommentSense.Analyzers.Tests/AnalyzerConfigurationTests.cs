using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class AnalyzerConfigurationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task AnalyzerOptionsInvalidValuesUseDefaults()
    {
        const string testCode = """
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                public void Method() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "not-an-int",
            ["comment_sense.require_ending_punctuation"] = "not-a-bool",
            ["comment_sense.similarity_threshold"] = "not-a-double"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task ParseSetEdgeCases()
    {
        const string testCode = """
            using System;
            /// <summary>This is a valid class summary.</summary>
            public class MyClass
            {
                /// <summary>This is a valid method summary.</summary>
                public void Method()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        var config = new Dictionary<string, string>
        {
            // Testing ParseSet with multiple commas, spaces, and empty segments
            ["comment_sense.ignored_exceptions"] = "  ArgumentNullException  , ,  System.Exception  "
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task CustomLowQualityTermsReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>TODO</summary>|}
                public void Method() { }
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
    public async Task PublicVisibilityLevelOnlyAnalyzesPublic()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:PublicMethod|}() { }
                protected void ProtectedMethod() { }
                internal void InternalMethod() { }
                private void PrivateMethod() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Public"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task ProtectedVisibilityLevelAnalyzesPublicAndProtected()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:PublicMethod|}() { }
                protected void {|CSENSE001:ProtectedMethod|}() { }
                internal void InternalMethod() { }
                private void PrivateMethod() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Protected"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task InternalVisibilityLevelAnalyzesPublicProtectedAndInternal()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:PublicMethod|}() { }
                protected void {|CSENSE001:ProtectedMethod|}() { }
                internal void {|CSENSE001:InternalMethod|}() { }
                private void PrivateMethod() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Internal"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task PrivateVisibilityLevelAnalyzesEverything()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:PublicMethod|}() { }
                protected void {|CSENSE001:ProtectedMethod|}() { }
                internal void {|CSENSE001:InternalMethod|}() { }
                private void {|CSENSE001:PrivateMethod|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Private"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
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

    [Test]
    public async Task ConstantFieldWithoutDocumentationReportsDiagnosticByDefault()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public const string {|CSENSE001:Version|} = "1.0";
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConstantFieldWithoutDocumentationDoesNotReportDiagnosticWhenExcluded()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public const string Version = "1.0";
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_constants"] = "true"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: config);
    }

    [Test]
    public async Task NonConstantFieldWithoutDocumentationStillReportsDiagnosticWhenConstantsExcluded()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public string {|CSENSE001:Version|} = "1.0";
            }
            """;

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_constants"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }
}
