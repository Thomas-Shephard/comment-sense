using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ConfigurationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public void AnalyzerOptionsGlobalFallback()
    {
        var localOptions = new MapOptions(new Dictionary<string, string>());
        var globalOptions = new MapOptions(new Dictionary<string, string>
        {
            ["comment_sense.min_summary_length"] = "15",
            ["comment_sense.require_ending_punctuation"] = "true",
            ["comment_sense.similarity_threshold"] = "0.75",
            ["comment_sense.low_quality_terms"] = "BAD, TERMS",
            ["comment_sense.ignored_exceptions"] = "Ex1, Ex2",
            ["comment_sense.analyze_internal"] = "true",
            ["comment_sense.allow_implicit_inheritdoc"] = "false"
        });

        var provider = new CustomProvider(localOptions, globalOptions);
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var options = AnalyzerOptions.GetOptions(provider, null!);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(options.MinSummaryLength, Is.EqualTo(15));
            Assert.That(options.RequireEndingPunctuation, Is.True);
            Assert.That(options.SimilarityThreshold, Is.EqualTo(0.75));
            Assert.That(options.LowQualityTerms, Contains.Item("BAD"));
            Assert.That(options.IgnoredExceptions, Contains.Item("Ex1"));
            Assert.That(options.AnalyzeInternal, Is.True);
            Assert.That(options.AllowImplicitInheritDoc, Is.False);
        }
    }

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

    private sealed class MapOptions(IDictionary<string, string> map) : AnalyzerConfigOptions
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        public override bool TryGetValue(string key, out string value) => map.TryGetValue(key, out value!);
    }

    private sealed class CustomProvider(AnalyzerConfigOptions local, AnalyzerConfigOptions global) : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions => global;
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => local;
        public override AnalyzerConfigOptions GetOptions(AdditionalText text) => local;
    }

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
