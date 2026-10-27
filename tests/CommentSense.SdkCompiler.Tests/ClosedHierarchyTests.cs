using CommentSense.CodeFixes.Logic;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class ClosedHierarchyTests
{
    [TestCase("class", false)]
    [TestCase("class", true)]
    [TestCase("record", false)]
    [TestCase("record", true)]
    public async Task RequiresSummary(string kind, bool documented)
    {
        var summary = documented ? "/// <summary>Represents a state.</summary>" : "";
        using var test = new CompilerTest($"{summary}\npublic closed {kind} State;");
        await test.VerifyAsync(documented ? [] : ["CSENSE001"]);
    }

    [Test]
    public async Task AddsPrimaryConstructorParameter()
    {
        const string source = """
            /// <summary>Represents a state.</summary>
            public closed record State;
            /// <summary>Represents a successful result.</summary>
            public sealed record Ready(int Value) : State;
            """;
        using var test = new CompilerTest(source);
        await test.VerifyAsync("CSENSE002");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE002");
        await test.VerifySourceAsync(source.Replace("public sealed record", "/// <param name=\"Value\">TODO</param>\npublic sealed record", StringComparison.Ordinal));
        await test.VerifyAsync();
    }

    [TestCase("", false)]
    [TestCase(" path=\"/summary\"", true)]
    public async Task InheritsOnlySelectedDocumentation(string path, bool missingParameter)
    {
        using var test = new CompilerTest($$"""
            /// <summary>Represents a state.</summary>
            /// <param name="value">The state value.</param>
            public closed class State(int value)
            {
                /// <summary>Gets the state value.</summary>
                /// <value>The stored value.</value>
                public int Value => value;
            }
            /// <inheritdoc cref="State"{{path}}/>
            public sealed class Ready(int value) : State(value);
            """);
        await test.VerifyAsync(missingParameter ? ["CSENSE002"] : []);
    }

    [Test]
    public async Task CombinesPartialDocumentation()
    {
        using var test = new CompilerTest("""
            /// <summary>Represents a state.</summary>
            public closed partial class State<T>;
            """);
        test.AddDocument("""
            /// <typeparam name="T">The state value type.</typeparam>
            public closed partial class State<T>;
            """);
        await test.VerifyAsync();
    }
}
