using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class DocumentationSynchronizationTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, DocumentationSynchronizationCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE016.severity", "none" }
    };

    [Test]
    public async Task RenameStrayParamToMatchUndocumentedParam()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// {|CSENSE003:<param name="p11">Documentation</param>|}
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// <param name="p1">Documentation</param>
                public void M(int p1) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RenameStrayTypeParamToMatchUndocumentedTypeParam()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// {|CSENSE005:<typeparam name="TT">Documentation</typeparam>|}
                public void M<{|CSENSE004:T|}>() { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// <typeparam name="T">Documentation</typeparam>
                public void M<T>() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task TriggerOnMissingParamToRenameStrayTag()
    {
        const string source = """
            using System.Diagnostics.CodeAnalysis;

            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// <param name="p11">Documentation</param>
                [SuppressMessage("CommentSense", "CSENSE003")]
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        const string fixedSource = """
            using System.Diagnostics.CodeAnalysis;

            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// <param name="p1">Documentation</param>
                [SuppressMessage("CommentSense", "CSENSE003")]
                public void M(int p1) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task DoesNotRenameIfSimilarityBelowThreshold()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// {|CSENSE003:<param name="somethingElse">Documentation</param>|}
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["comment_sense.rename_similarity_threshold"] = "0.9"
        };

        await VerifyCodeFixAsync(source, source, options);
    }

    [Test]
    public async Task RenameStrayTagInLastTriviaBlock()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Orphaned doc</summary>
                // Intervening comment
                /// <summary>Summary of the method M.</summary>
                /// {|CSENSE003:<param name="p11">Documentation</param>|}
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Orphaned doc</summary>
                // Intervening comment
                /// <summary>Summary of the method M.</summary>
                /// <param name="p1">Documentation</param>
                public void M(int p1) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task FixAllRenamesMultipleStrayTags()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// {|CSENSE003:<param name="p11">Doc 1</param>|}
                /// {|CSENSE003:<param name="p22">Doc 2</param>|}
                public void M(int {|CSENSE002:p1|}, int {|CSENSE002:p2|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary of the method M.</summary>
                /// <param name="p1">Doc 1</param>
                /// <param name="p2">Doc 2</param>
                public void M(int p1, int p2) { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task FixAllDeterministicMatching()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="count">Size of collection</param>|}
                /// {|CSENSE003:<param name="itemsX">Collection items</param>|}
                public void Process(int {|CSENSE002:counter|}, string[] {|CSENSE002:items|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="counter">Size of collection</param>
                /// <param name="items">Collection items</param>
                public void Process(int counter, string[] items) { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RenameStrayTagWithTextAttribute()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="p 11">Documentation</param>|}
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">Documentation</param>
                public void M(int p1) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }
}
