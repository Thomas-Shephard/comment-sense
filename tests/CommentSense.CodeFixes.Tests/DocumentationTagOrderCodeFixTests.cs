using System.Collections.Immutable;
using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class DocumentationTagOrderCodeFixTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, TagOrderCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE002.severity", "none" },
        { "dotnet_diagnostic.CSENSE004.severity", "none" },
        { "dotnet_diagnostic.CSENSE006.severity", "none" },
        { "dotnet_diagnostic.CSENSE016.severity", "none" },
        { "dotnet_diagnostic.CSENSE026.severity", "none" }
    };

    [Test]
    public async Task InheritDocIsMovedToFirst()
    {
        const string testCode = """
            /// <summary>Documentation.</summary>
            /// <inheritdoc />
            public class MyClass { }
            """;
        const string fixedCode = """
            /// <inheritdoc />
            /// <summary>Documentation.</summary>
            public class MyClass { }
            """;

        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(2, 5, 2, 19)
            .WithArguments("inheritdoc", "summary");

        await VerifyCodeFixAsync(testCode, fixedCode, DisableUnrelatedRules, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task MultipleTagsAreReordered()
    {
        const string testCode = """
            /// <remarks>Documentation.</remarks>
            /// <exception cref="System.Exception">Documentation.</exception>
            /// <summary>Documentation.</summary>
            /// <param name="p">Documentation.</param>
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;
        const string fixedCode = """
            /// <summary>Documentation.</summary>
            /// <param name="p">Documentation.</param>
            /// <exception cref="System.Exception">Documentation.</exception>
            /// <remarks>Documentation.</remarks>
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(2, 5, 2, 66)
            .WithArguments("exception", "remarks");

        await VerifyCodeFixAsync(testCode, fixedCode, DisableUnrelatedRules, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task InternalOrderOfSameTagsIsPreserved()
    {
        const string testCode = """
            /// <exception cref="System.ArgumentException">Documentation.</exception>
            /// <exception cref="System.InvalidOperationException">Documentation.</exception>
            /// <summary>Documentation.</summary>
            public class MyClass { }
            """;
        const string fixedCode = """
            /// <summary>Documentation.</summary>
            /// <exception cref="System.ArgumentException">Documentation.</exception>
            /// <exception cref="System.InvalidOperationException">Documentation.</exception>
            public class MyClass { }
            """;

        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(3, 5, 3, 38)
            .WithArguments("summary", "exception");

        await VerifyCodeFixAsync(testCode, fixedCode, DisableUnrelatedRules, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task CustomTagOrderIsRespected()
    {
        const string testCode = """
            /// <summary>Documentation.</summary>
            /// <remarks>Documentation.</remarks>
            /// <param name="p">Documentation.</param>
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;
        const string fixedCode = """
            /// <summary>Documentation.</summary>
            /// <param name="p">Documentation.</param>
            /// <remarks>Documentation.</remarks>
            public class MyClass
            {
                public void MyMethod(int p) { }
            }
            """;

        var config = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "comment_sense.tag_order", "summary, param, remarks" }
        };

        var expectedFinal = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(3, 5, 3, 43)
            .WithArguments("param", "remarks");

        await VerifyCodeFixAsync(testCode, fixedCode, config, expectedDiagnostics: [expectedFinal]);
    }

    [Test]
    public async Task CustomTagOrderWithoutFallbackIsRespected()
    {
        const string testCode = """
            /// <summary>Documentation.</summary>
            /// <remarks>Documentation.</remarks>
            public class MyClass { }
            """;
        const string fixedCode = """
            /// <remarks>Documentation.</remarks>
            /// <summary>Documentation.</summary>
            public class MyClass { }
            """;

        var config = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            // Only 'remarks' is in the list, so it has priority 0.
            // 'summary' is not in the list, so it should be priority 100.
            { "comment_sense.tag_order", "remarks" }
        };

        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(2, 5, 2, 38)
            .WithArguments("remarks", "summary");

        await VerifyCodeFixAsync(testCode, fixedCode, config, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task EmptyTagNameDoesNotCrash()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            /// < >Something</ >
            public class MyClass { }
            """;

        await VerifyCodeFixAsync(testCode, testCode, DisableUnrelatedRules);
    }

    [Test]
    public async Task MultiLineDocumentationTagsAreReordered()
    {
        const string testCode = """
            /**
             * <remarks>Documentation.</remarks>
             * <summary>Documentation.</summary>
             */
            public class MyClass { }
            """;
        const string fixedCode = """
            /**
             * <summary>Documentation.</summary>
             * <remarks>Documentation.</remarks>
             */
            public class MyClass { }
            """;

        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.DocumentationTagOrderMismatchId, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning)
            .WithSpan(3, 4, 3, 37)
            .WithArguments("summary", "remarks");

        await VerifyCodeFixAsync(testCode, fixedCode, DisableUnrelatedRules, expectedDiagnostics: [expected]);
    }

    [Test]
    public void GetTagPriorityEmptyTagNameReturnsDefaultPriority()
    {
        var name = SyntaxFactory.XmlName(SyntaxFactory.Identifier(""));
        var tag = SyntaxFactory.XmlEmptyElement(name);
        var tagOrder = new Dictionary<string, int>().ToImmutableDictionary();

        var priority = TagOrderCodeFixProvider.GetTagPriority(tag, tagOrder);

        Assert.That(priority, Is.EqualTo(100));
    }

    [Test]
    public void GetTagPriorityUnknownTagNameReturnsDefaultPriority()
    {
        var tag = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("unknown"));
        var tagOrder = new Dictionary<string, int>().ToImmutableDictionary();

        var priority = TagOrderCodeFixProvider.GetTagPriority(tag, tagOrder);

        Assert.That(priority, Is.EqualTo(100));
    }
}
