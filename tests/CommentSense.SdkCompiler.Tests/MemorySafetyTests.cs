using CommentSense.CodeFixes.Logic;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class MemorySafetyTests
{
    private const string Source = """
        /// <summary>Reads unmanaged memory.</summary>
        public class Memory
        {
            /// <summary>Reads a stored value.</summary>
            /// <param name="address">The memory address.</param>
            public int Read(int* address) => unsafe(*address);
        }
        """;

    [TestCase(false)]
    [TestCase(true)]
    public async Task AddsReturnDocumentation(bool updatedRules)
    {
        using var test = new CompilerTest(Source, "preview", updatedRules);
        await test.VerifyAsync("CSENSE006");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE006");
        await test.VerifySourceAsync(Source.Replace("    public int Read", "    /// <returns>TODO</returns>\n    public int Read", StringComparison.Ordinal));
        await test.VerifyAsync();
    }

    [Test]
    public async Task UnsafeExpressionRequiresPreview()
    {
        using var test = new CompilerTest(Source);
        var compilation = await test.Document.Project.GetCompilationAsync();
        Assert.That(compilation, Is.Not.Null);
        var errors = compilation?.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        Assert.That(errors, Is.Not.Empty);
        string[] expected = ["CS8652", "CS0214"];
        Assert.That(errors?.Select(d => d.Id), Is.EquivalentTo(expected));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SupportsPointerRelaxations(bool updatedRules)
    {
        using var test = new CompilerTest("""
            /// <summary>Works with native storage.</summary>
            public class Memory
            {
                /// <summary>Prepares native storage.</summary>
                public void Prepare()
                {
                    int value = 0;
                    int* address = &value;
                    int* buffer = stackalloc int[2];
                    int size = sizeof(System.Guid);
                    fixed (int* pinned = new int[2]) { }
                }
            }
            """, "preview", updatedRules);
        await test.VerifyAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DocumentsSafeExternMembers(bool updatedRules)
    {
        using var test = new CompilerTest("""
            /// <summary>Provides native services.</summary>
            public class Native
            {
                [System.Runtime.InteropServices.DllImport("native")]
                public static safe extern int Read();
            }
            """, "preview", updatedRules);
        await test.VerifyAsync("CSENSE001");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE001");
        await test.VerifyAsync("CSENSE006");
        await test.FixAsync(new ContentGenerationCodeFixProvider(), "CSENSE006");
        await test.VerifySourceAsync("""
            /// <summary>Provides native services.</summary>
            public class Native
            {
                /// <summary>TODO</summary>
                /// <returns>TODO</returns>
                [System.Runtime.InteropServices.DllImport("native")]
                public static safe extern int Read();
            }
            """);
        await test.VerifyAsync();
    }

    [TestCase(false, false)]
    [TestCase(false, true)]
    [TestCase(true, false)]
    [TestCase(true, true)]
    public async Task AttributesInitializerExceptionsToConstruction(bool explicitConstructor, bool updatedRules)
    {
        var constructor = explicitConstructor ? "/// <summary>Creates native storage.</summary>\npublic Memory() { }" : "";
        using var test = new CompilerTest($$"""
            /// <summary>Owns native storage.</summary>
            public class Memory
            {
                private readonly int value = unsafe(Read());
                /// <summary>Reads a stored value.</summary>
                /// <returns>The value.</returns>
                /// <exception cref="System.ArgumentException">Storage is invalid.</exception>
                private static unsafe int Read() => throw new System.ArgumentException();
                {{constructor}}
            }
            """, "preview", updatedRules);
        await test.VerifyAsync("CSENSE012");
        var diagnostic = (await test.AnalyzeAsync()).Single();
        Assert.That((await test.Document.GetTextAsync()).ToString(diagnostic.Location.SourceSpan), Is.EqualTo("Memory"));
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task PropagatesUnsafeCallExceptions(bool updatedRules)
    {
        using var test = new CompilerTest("""
            /// <summary>Uses native operations.</summary>
            public class NativeCaller
            {
                /// <summary>Performs a native operation.</summary>
                /// <exception cref="System.ArgumentException">The operation fails.</exception>
                private static unsafe int Source() => throw new System.ArgumentException();
                /// <summary>Reads a native result.</summary>
                /// <returns>The result.</returns>
                public int Read() => unsafe(Source());
            }
            """, "preview", updatedRules);
        await test.VerifyAsync("CSENSE012");
    }
}
