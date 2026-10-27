using CommentSense.CodeFixes.Logic;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class DocumentationFixTests
{
    [TestCase("union", "union Choice(int, string)")]
    [TestCase("closed", "closed class State")]
    public async Task FormatsConfiguredKeywords(string keyword, string declaration)
    {
        using var test = new CompilerTest($"/// <summary>Represents a {keyword} type.</summary>\npublic {declaration};",
            options: new Dictionary<string, string> { ["comment_sense.langwords"] = keyword });
        await test.VerifyAsync("CSENSE019");
        await test.FixAsync(new KeywordToSeeLangwordCodeFixProvider(), "CSENSE019");
        await test.VerifySourceAsync($"/// <summary>Represents a <see langword=\"{keyword}\" /> type.</summary>\npublic {declaration};");
        await test.VerifyAsync();
    }

    [Test]
    public async Task AddsUnionTypeParameterWithoutRemovingOtherDiagnostics()
    {
        using var test = new CompilerTest("""
            /// <summary>Represents a result.</summary>
            public union Result<T>(T, string);
            public closed class State;
            """);
        await test.VerifyAsync("CSENSE004", "CSENSE001");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE004");
        await test.VerifySourceAsync("""
            /// <summary>Represents a result.</summary>
            /// <typeparam name="T">TODO</typeparam>
            public union Result<T>(T, string);
            public closed class State;
            """);
        await test.VerifyAsync("CSENSE001");
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OrdersUnionTags(bool fixAll)
    {
        using var test = new CompilerTest("""
            /// <typeparam name="T">The success type.</typeparam>
            /// <summary>Represents an operation result.</summary>
            public union Result<T>(T, string);
            """);
        await test.VerifyAsync("CSENSE024");
        await test.FixAsync(new TagOrderCodeFixProvider(), "CSENSE024", fixAll ? FixAllScope.Document : null);
        await test.VerifySourceAsync("""
            /// <summary>Represents an operation result.</summary>
            /// <typeparam name="T">The success type.</typeparam>
            public union Result<T>(T, string);
            """);
        await test.VerifyAsync();
    }

    [Test]
    public async Task AddsInheritedDocumentationToClosedHierarchyMember()
    {
        const string source = """
            /// <summary>Represents a state.</summary>
            public closed class State
            {
                /// <summary>Performs an operation.</summary>
                public abstract void Execute();
            }
            /// <summary>Represents a ready state.</summary>
            public sealed class Ready : State
            {
                public override void Execute() { }
            }
            """;
        using var test = new CompilerTest(source, options: new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "false"
        });
        await test.VerifyAsync("CSENSE018");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE018");
        await test.VerifySourceAsync(source.Replace("    public override", "    /// <inheritdoc />\n    public override", StringComparison.Ordinal));
        await test.VerifyAsync();
    }
}
