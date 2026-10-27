using CommentSense.CodeFixes.Logic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class UnionTests
{
    [Test]
    public async Task UsesSdkCompilerAndWorkspace()
    {
        TestContext.Out.WriteLine(typeof(CSharpCompilation).Assembly.FullName);
        TestContext.Out.WriteLine(typeof(Workspace).Assembly.FullName);
        Assert.That(typeof(CSharpCompilation).Assembly.GetName().Version,
            Is.EqualTo(typeof(Workspace).Assembly.GetName().Version));
        using var test = new CompilerTest("public union Choice(int, string);");
        await test.VerifyAsync("CSENSE001");
    }

    [Test]
    public async Task AddsSummary()
    {
        using var test = new CompilerTest("public union Choice(int, string);");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE001");
        await test.VerifySourceAsync("""
            /// <summary>TODO</summary>
            public union Choice(int, string);
            """);
        await test.VerifyAsync();
    }

    [TestCase("public", "protected", "CSENSE001")]
    [TestCase("internal", "protected", null)]
    [TestCase("internal", "private", "CSENSE001")]
    public async Task RespectsVisibility(string accessibility, string visibility, string? diagnostic)
    {
        using var test = new CompilerTest($"{accessibility} union Choice(int, string);",
            options: new Dictionary<string, string> { ["comment_sense.visibility_level"] = visibility });
        await test.VerifyAsync(diagnostic is null ? [] : [diagnostic]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task RequiresTypeParameterDocumentation(bool documented)
    {
        var tag = documented ? "/// <typeparam name=\"T\">The success type.</typeparam>" : "";
        using var test = new CompilerTest($"""
            /// <summary>Represents an operation result.</summary>
            {tag}
            public union Result<T>(T, string);
            """);
        await test.VerifyAsync(documented ? [] : ["CSENSE004"]);
    }

    [Test]
    public async Task GeneratedMembersDoNotRequireDocumentation()
    {
        using var test = new CompilerTest("""
            /// <summary>Represents either supported value.</summary>
            public union Choice(int, string);
            """, options: new Dictionary<string, string> { ["comment_sense.visibility_level"] = "private" });
        await test.VerifyAsync();
        var compilation = await test.Document.Project.GetCompilationAsync();
        var type = compilation?.GetTypeByMetadataName("Choice");
        Assert.That(type, Is.Not.Null);
        Assert.That(type?.GetMembers().Any(member => member.IsImplicitlyDeclared), Is.True);
    }

    [TestCase("Choice", null)]
    [TestCase("Missing", "CSENSE007")]
    public async Task ResolvesReferences(string reference, string? diagnostic)
    {
        using var test = new CompilerTest($"""
            /// <summary>Represents a selection; see <see cref="{reference}"/>.</summary>
            public union Choice(int, string);
            """);
        await test.VerifyAsync(diagnostic is null ? [] : [diagnostic]);
    }

    [Test]
    public async Task AnalyzesUserDeclaredMembers()
    {
        using var test = new CompilerTest("""
            /// <summary>Represents either supported value.</summary>
            public union Choice(int, string)
            {
                public int Length => 0;
                /// <summary>Reads the selection.</summary>
                /// <returns>The selected value.</returns>
                public int Read() => throw new System.ArgumentException();
            }
            """);
        await test.VerifyAsync("CSENSE001", "CSENSE012");
    }

    [Test]
    public async Task CombinesPartialUnionDocumentation()
    {
        using var test = new CompilerTest("""
            /// <summary>Represents either supported value.</summary>
            public partial union Choice<T>(T, string);
            """);
        test.AddDocument("""
            /// <typeparam name="T">The value type.</typeparam>
            public partial union Choice<T>;
            """);
        await test.VerifyAsync();
    }

    [TestCase("=> new Choice(1);", true, true)]
    [TestCase("=> 1;", true, true)]
    [TestCase("=> (Choice)1;", true, true)]
    [TestCase("=> System.DateTime.Now.Ticks > 0 ? 1 : 2;", true, true)]
    [TestCase("{ Choice choice = 1; return choice; }", true, true)]
    [TestCase("{ Choice? choice = 1; return choice.Value; }", true, true)]
    [TestCase("{ Choice[] choices = [1]; return choices[0]; }", true, true)]
    [TestCase("{ Choice choice; choice = 1; return choice; }", true, true)]
    [TestCase("=> Consume(1);", true, true)]
    [TestCase("=> 1;", false, false)]
    [TestCase("{ try { return 1; } catch (System.ArgumentException) { return default; } }", true, false)]
    [TestCase("{ System.Func<Choice> create = () => 1; return default; }", true, false)]
    public async Task CustomUnionConstructionPropagatesExceptions(string body, bool scan, bool reports)
    {
        using var test = new CompilerTest($$"""
            /// <summary>Represents an integer choice.</summary>
            [System.Runtime.CompilerServices.Union]
            public struct Choice : System.Runtime.CompilerServices.IUnion
            {
                /// <summary>Creates a selection.</summary>
                /// <param name="value">The selected value.</param>
                /// <exception cref="System.ArgumentException">Invalid value.</exception>
                public Choice(int value) => throw new System.ArgumentException();
                /// <summary>Gets the selection.</summary>
                /// <value>The selected value.</value>
                public object Value => 0;
            }
            /// <summary>Creates choices.</summary>
            public class Factory
            {
                /// <summary>Creates a selection.</summary>
                /// <returns>The selection.</returns>
                public Choice Create() {{body}}
                private static Choice Consume(Choice value) => value;
            }
            """, options: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
        await test.VerifyAsync(reports ? ["CSENSE012"] : []);
    }

    [TestCase("=> 1;", true, true)]
    [TestCase("=> (Choice)1;", true, true)]
    [TestCase("=> Choice.IUnionMembers.Create(1);", true, true)]
    [TestCase("{ Choice[] choices = [1]; return choices[0]; }", true, true)]
    [TestCase("=> 1;", false, false)]
    [TestCase("=> (Choice)1;", false, false)]
    [TestCase("{ try { return 1; } catch (System.ArgumentException) { return null; } }", true, false)]
    [TestCase("{ System.Func<Choice> create = () => 1; return null; }", true, false)]
    [TestCase("{ System.Func<int, Choice> create = Choice.IUnionMembers.Create; return null; }", true, false)]
    [TestCase("{ var create = (System.Func<int, Choice>)Choice.IUnionMembers.Create; return null; }", true, false)]
    public async Task UnionFactoryConversionPropagatesExceptions(string body, bool scan, bool reports)
    {
        using var test = new CompilerTest($$"""
            /// <summary>Represents a choice.</summary>
            [System.Runtime.CompilerServices.Union]
            public class Choice : Choice.IUnionMembers
            {
                /// <summary>Provides union operations.</summary>
                public interface IUnionMembers
                {
                    /// <summary>Creates an integer choice.</summary>
                    /// <param name="value">The chosen value.</param>
                    /// <returns>The choice.</returns>
                    /// <exception cref="System.ArgumentException">Invalid value.</exception>
                    public static Choice Create(int value) => throw new System.ArgumentException();
                    /// <summary>Creates a text choice.</summary>
                    /// <param name="value">The chosen text.</param>
                    /// <returns>The choice.</returns>
                    /// <exception cref="System.NotSupportedException">Text is unsupported.</exception>
                    public static Choice Create(string value) => throw new System.NotSupportedException();
                    /// <summary>Gets the selection.</summary>
                    /// <value>The selected value.</value>
                    public object Value { get; }
                }
                /// <inheritdoc />
                object IUnionMembers.Value => 0;
            }
            /// <summary>Creates choices.</summary>
            public class Factory
            {
                /// <summary>Creates a selection.</summary>
                /// <returns>The selection.</returns>
                public Choice Create() {{body}}
            }
            """, options: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
        await test.VerifyAsync(reports ? ["CSENSE012"] : []);
        if (reports)
        {
            var diagnostic = (await test.AnalyzeAsync()).Single();
            Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Does.Contain("ArgumentException"));
            await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE012");
            await test.VerifyAsync();
        }
    }

    [TestCase(FixAllScope.Document)]
    [TestCase(FixAllScope.Project)]
    [TestCase(FixAllScope.Solution)]
    public async Task FixAllDocumentsEachUnionOnce(FixAllScope scope)
    {
        using var test = new CompilerTest("public union First(int, string);\npublic union Second(int, string);");
        if (scope != FixAllScope.Document)
            test.AddDocument("public union Third(int, string);");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE001", scope);
        await test.VerifySourceAsync("""
            /// <summary>TODO</summary>
            public union First(int, string);
            /// <summary>TODO</summary>
            public union Second(int, string);
            """);
        await test.VerifyAsync();
        if (scope != FixAllScope.Document)
        {
            var other = test.Document.Project.Documents.Single(document => document.Id != test.Document.Id);
            Assert.That((await other.GetTextAsync()).ToString(), Is.EqualTo(CompilerTest.Normalize("/// <summary>TODO</summary>\npublic union Third(int, string);")));
        }
        await test.VerifyFixAllUnchangedAsync(new ContentGenerationCodeFixProvider(), "CSENSE001", scope);
    }

    [TestCase("public union Choice(int,")]
    [TestCase("public closed class State {")]
    [TestCase("public static class Access { extension(int[] values) { public int this[int")]
    public async Task IncompleteDeclarationsDoNotCrash(string source)
    {
        using var test = new CompilerTest(source);
        var diagnostics = await test.AnalyzeAsync(requireValidSource: false);
        Assert.That(diagnostics.All(d => d.Id.StartsWith("CSENSE", StringComparison.Ordinal)), Is.True);
    }
}
