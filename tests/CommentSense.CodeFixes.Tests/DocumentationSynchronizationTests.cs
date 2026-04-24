using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;
using System.Reflection;

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

    [Test]
    public async Task RenameStraySelfClosingParamTag()
    {
        const string source = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="p11" />|}
                public void M(int {|CSENSE002:p1|}) { }
            }
            """;

        const string fixedSource = """
            /// <summary>Test class.</summary>
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1" />
                public void M(int p1) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task FixDocumentAsyncReturnsDocumentForUnsupportedXmlNodeKind()
    {
        const string source = """
            public class Test
            {
                /// <summary>Documentation</summary>
                public void M(int value) { }
            }
            """;

        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", source);
        var root = await document.GetSyntaxRootAsync() ?? throw new InvalidOperationException();
        var xmlText = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlTextSyntax>().First(x => x.ToString().Contains("Documentation"));
        var symbol = await document.GetSemanticModelAsync() is { } semanticModel
            ? semanticModel.GetDeclaredSymbol(root.DescendantNodes().OfType<MethodDeclarationSyntax>().First())
            : null;

        var match = new DocumentationSynchronizationLogic.MatchResult(
            xmlText,
            "param",
            "value1",
            "value",
            1.0,
            symbol ?? throw new InvalidOperationException());

        var method = typeof(DocumentationSynchronizationCodeFixProvider).GetMethod("FixDocumentAsync", BindingFlags.NonPublic | BindingFlags.Static)
                     ?? throw new InvalidOperationException();

        var invoked = method.Invoke(null, [document, match, CancellationToken.None]);
        var task = invoked as Task<Document> ?? throw new InvalidOperationException();
        var result = await task;

        Assert.That(result, Is.EqualTo(document));
    }
}
