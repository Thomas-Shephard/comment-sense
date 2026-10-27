using CommentSense.CodeFixes.Logic;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class ExtensionIndexerTests
{
    private const string Source = """
        /// <summary>Provides sequence access.</summary>
        public static class Access
        {
            /// <summary>Extends integer sequences.</summary>
            /// <param name="values">The sequence to access.</param>
            extension(int[] values)
            {
                /// <summary>Gets an entry from <paramref name="values"/>.</summary>
                /// <param name="index">The entry position.</param>
                /// <value>The requested entry.</value>
                public int this[int index] => values[index];
            }
        }
        """;

    [Test]
    public async Task DocumentedIndexerAndReceiverAreValid()
    {
        using var test = new CompilerTest(Source);
        await test.VerifyAsync();
    }

    [TestCase("<param name=\"index\">The entry position.</param>", "CSENSE002")]
    [TestCase("<value>The requested entry.</value>", "CSENSE014")]
    public async Task RequiresMemberDocumentation(string tag, string id)
    {
        using var test = new CompilerTest(Source.Replace(tag, "", StringComparison.Ordinal));
        await test.VerifyAsync(id);
    }

    [Test]
    public async Task BlockDiagnosticHasReadableNameAndLocation()
    {
        using var test = new CompilerTest(Source
            .Replace("<summary>Extends integer sequences.</summary>", "", StringComparison.Ordinal)
            .Replace("<param name=\"values\">The sequence to access.</param>", "", StringComparison.Ordinal));
        var diagnostics = await test.AnalyzeAsync();
        Assert.That(diagnostics, Has.Length.EqualTo(1));
        var diagnostic = diagnostics[0];
        using var scope = Assert.EnterMultipleScope();
        Assert.That(diagnostic.Id, Is.EqualTo("CSENSE001"));
        Assert.That(diagnostic.GetMessage(System.Globalization.CultureInfo.InvariantCulture), Does.Contain("Access.extension(int[])"));
        Assert.That((await test.Document.GetTextAsync()).ToString(diagnostic.Location.SourceSpan), Is.EqualTo("extension"));
    }

    [Test]
    public async Task RequiresIndexerDocumentation()
    {
        var start = Source.IndexOf("        /// <summary>Gets", StringComparison.Ordinal);
        var end = Source.IndexOf("        public int this", StringComparison.Ordinal);
        using var test = new CompilerTest(Source.Remove(start, end - start));
        await test.VerifyAsync("CSENSE001");
    }

    [TestCase("", false)]
    [TestCase(" path=\"/summary\"", true)]
    public async Task InheritsSelectedTags(string path, bool incomplete)
    {
        const string reference = """
            /// <summary>Provides reference access.</summary>
            public class Reference
            {
                /// <summary>Gets an entry.</summary>
                /// <param name="index">The entry position.</param>
                /// <value>The requested entry.</value>
                public int this[int index] => index;
            }
            """;
        var start = Source.IndexOf("        /// <summary>Gets", StringComparison.Ordinal);
        var end = Source.IndexOf("        public int this", StringComparison.Ordinal);
        var inherited = Source[..start] + $"        /// <inheritdoc cref=\"Reference.this[int]\"{path}/>\n" + Source[end..];
        using var test = new CompilerTest(reference + "\n" + inherited);
        await test.VerifyAsync(incomplete ? ["CSENSE002", "CSENSE014"] : []);
    }

    [TestCase("receiver[0]", "int", false)]
    [TestCase("receiver?[0]", "int?", false)]
    [TestCase("receiver[0] = 1", "int", true)]
    public async Task PropagatesAccessorExceptions(string access, string returnType, bool setter)
    {
        var body = setter ? "{ get => 0; set => throw new System.ArgumentException(); }" : "=> throw new System.ArgumentException();";
        using var test = new CompilerTest($$"""
            /// <summary>Receives indexed calls.</summary>
            public class Receiver;
            /// <summary>Provides indexed access.</summary>
            public static class Access
            {
                /// <summary>Extends the receiver.</summary>
                /// <param name="receiver">The receiver.</param>
                extension(Receiver receiver)
                {
                    /// <summary>Gets an entry.</summary>
                    /// <param name="index">The entry position.</param>
                    /// <value>The requested entry.</value>
                    /// <exception cref="System.ArgumentException">Invalid position.</exception>
                    public int this[int index] {{body}}
                }
            }
            /// <summary>Reads indexed values.</summary>
            public class Reader
            {
                /// <summary>Reads an entry.</summary>
                /// <param name="receiver">The receiver.</param>
                /// <returns>The requested entry.</returns>
                public {{returnType}} Read(Receiver receiver) => {{access}};
            }
            """);
        await test.VerifyAsync("CSENSE012");
    }

    [TestCase("=> storage is [0];", true, true)]
    [TestCase("=> storage[0] == 0;", true, true)]
    [TestCase("=> storage switch { [0] => true, _ => false };", true, true)]
    [TestCase("=> storage is [0, ..];", true, true)]
    [TestCase("=> storage is [0, .. var rest];", true, true, true)]
    [TestCase("=> storage is [];", true, false)]
    [TestCase("=> storage is [0];", false, false)]
    [TestCase("{ try { return storage is [0]; } catch (System.ArgumentException) { return false; } }", true, false)]
    [TestCase("{ System.Func<bool> match = () => storage is [0]; return false; }", true, false)]
    public async Task ListPatternPropagatesExtensionIndexerExceptions(string body, bool scan, bool reports, bool slices = false)
    {
        using var test = new CompilerTest($$"""
            /// <summary>Stores values.</summary>
            public class Storage
            {
                /// <summary>Gets the length.</summary>
                /// <value>The length.</value>
                public int Length => 1;
            }
            /// <summary>Provides indexed access.</summary>
            public static class Access
            {
                /// <summary>Extends storage.</summary>
                /// <param name="storage">The storage.</param>
                extension(Storage storage)
                {
                    /// <summary>Gets a value.</summary>
                    /// <param name="index">The position.</param>
                    /// <value>The stored value.</value>
                    /// <exception cref="System.ArgumentException">Invalid position.</exception>
                    public int this[int index] => throw new System.ArgumentException();
                    /// <summary>Gets a slice.</summary>
                    /// <param name="range">The positions.</param>
                    /// <value>The slice.</value>
                    /// <exception cref="System.NotSupportedException">Slicing is unsupported.</exception>
                    public Storage this[System.Range range] => throw new System.NotSupportedException();
                }
            }
            /// <summary>Matches storage.</summary>
            public class Consumer
            {
                /// <summary>Matches stored values.</summary>
                /// <param name="storage">The storage.</param>
                /// <returns>Whether the values match.</returns>
                public bool Match(Storage storage) {{body}}
            }
            """, options: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
        await test.VerifyAsync(slices ? ["CSENSE012", "CSENSE012"] : reports ? ["CSENSE012"] : []);
        if (reports)
        {
            await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE012");
            if (slices)
                await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE012");
            await test.VerifyAsync();
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ChecksGenericBlockDocumentation(bool documented)
    {
        var typeParameter = documented ? "/// <typeparam name=\"T\">The element type.</typeparam>" : "";
        using var test = new CompilerTest($$"""
            /// <summary>Provides sequence access.</summary>
            public static class Access
            {
                /// <summary>Extends sequences.</summary>
                {{typeParameter}}
                /// <param name="values">The sequence to access.</param>
                extension<T>(T[] values)
                {
                    /// <summary>Gets an entry of type <typeparamref name="T"/>.</summary>
                    /// <param name="index">The entry position.</param>
                    /// <value>The requested entry.</value>
                    public T this[int index] => values[index];
                }
            }
            """);
        await test.VerifyAsync(documented ? [] : ["CSENSE004"]);
    }

    [Test]
    public async Task ReportsSetterExceptions()
    {
        var source = Source.Replace("=> values[index];", "{ get => values[index]; set => throw new System.ArgumentException(); }", StringComparison.Ordinal);
        using var test = new CompilerTest(source);
        await test.VerifyAsync("CSENSE012");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE012");
        await test.VerifySourceAsync(source.Replace("        public int this", "        /// <exception cref=\"System.ArgumentException\">TODO</exception>\n        public int this", StringComparison.Ordinal));
        await test.VerifyAsync();
    }

    [TestCase("<param name=\"index\">The entry position.</param>", "<param name=\"index\">TODO</param>", "CSENSE002")]
    [TestCase("<value>The requested entry.</value>", "<value>TODO</value>", "CSENSE014")]
    public async Task AddsMissingTag(string tag, string replacement, string id)
    {
        var source = Source.ReplaceLineEndings("\n").Replace("        /// " + tag + "\n", "", StringComparison.Ordinal);
        using var test = new CompilerTest(source);
        await test.FixAsync(new ContentGenerationCodeFixProvider(), id);
        await test.VerifySourceAsync(Source.Replace(tag, replacement, StringComparison.Ordinal));
        await test.VerifyAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task OrdersIndexerTags(bool fixAll)
    {
        const string parameter = "        /// <param name=\"index\">The entry position.</param>";
        const string value = "        /// <value>The requested entry.</value>";
        var source = Source.ReplaceLineEndings("\n").Replace(parameter + "\n" + value, value + "\n" + parameter, StringComparison.Ordinal);
        using var test = new CompilerTest(source);
        await test.VerifyAsync("CSENSE024");
        await test.FixAsync(new TagOrderCodeFixProvider(), "CSENSE024", fixAll ? FixAllScope.Document : null);
        await test.VerifySourceAsync(Source);
        await test.VerifyAsync();
    }
}
