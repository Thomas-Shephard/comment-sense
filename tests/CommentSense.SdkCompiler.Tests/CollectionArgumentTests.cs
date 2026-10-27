using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class CollectionArgumentTests
{
    private const string Collection = """
        /// <summary>Stores integer entries.</summary>
        public class Bag : System.Collections.Generic.List<int>
        {
            /// <summary>Creates empty storage.</summary>
            /// <exception cref="System.InvalidOperationException">Storage is unavailable.</exception>
            public Bag() { }
            /// <summary>Allocates entry storage.</summary>
            /// <param name="capacity">The storage capacity.</param>
            /// <exception cref="System.ArgumentException">The capacity is invalid.</exception>
            public Bag(int capacity) { }
        }
        """;

    private const string Builder = """
        /// <summary>Stores entries.</summary>
        [System.Runtime.CompilerServices.CollectionBuilder(typeof(Builder), "Create")]
        public class Bag : System.Collections.Generic.List<int>;
        /// <summary>Constructs entry storage.</summary>
        public static class Builder
        {
            /// <summary>Creates empty storage.</summary>
            /// <param name="values">The initial entries.</param>
            /// <returns>The resulting storage.</returns>
            /// <exception cref="System.InvalidOperationException">Storage is unavailable.</exception>
            public static Bag Create(System.ReadOnlySpan<int> values) => new();
            /// <summary>Constructs entry storage.</summary>
            /// <param name="capacity">The storage capacity.</param>
            /// <param name="values">The initial entries.</param>
            /// <returns>The resulting storage.</returns>
            /// <exception cref="System.ArgumentException">The capacity is invalid.</exception>
            public static Bag Create(int capacity, System.ReadOnlySpan<int> values) => new();
        }
        """;

    [TestCase(false, true, false)]
    [TestCase(false, false, false)]
    [TestCase(false, true, true)]
    [TestCase(true, true, false)]
    [TestCase(true, false, false)]
    [TestCase(true, true, true)]
    public async Task UsesSelectedConstructorOrBuilder(bool builder, bool scan, bool documented)
    {
        var exceptionTag = documented ? "/// <exception cref=\"System.ArgumentException\">Invalid capacity.</exception>" : "";
        using var test = Create(builder, "=> [with(-1), 1];", scan, exceptionTag);
        await test.VerifyAsync(scan && !documented ? ["CSENSE012"] : []);
        if (scan && !documented)
        {
            var diagnostics = await test.AnalyzeAsync();
            Assert.That(diagnostics[0].GetMessage(System.Globalization.CultureInfo.InvariantCulture), Does.Contain("ArgumentException").And.Not.Contain("InvalidOperationException"));
        }
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task CaughtConstructionExceptionIsExcluded(bool builder)
    {
        using var test = Create(builder, "{ try { return [with(-1), 1]; } catch (System.ArgumentException) { return null; } }");
        await test.VerifyAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task DeferredConstructionIsExcluded(bool builder)
    {
        using var test = Create(builder, "{ System.Func<Bag> create = () => [with(-1), 1]; return null; }");
        await test.VerifyAsync();
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ArgumentThrowIsIndependentOfCalledMethodScanning(bool scan)
    {
        using var test = Create(false, "=> [with(System.DateTime.Now.Ticks > 0 ? -1 : throw new System.NotSupportedException()), 1];", scan);
        await test.VerifyAsync(scan ? ["CSENSE012", "CSENSE012"] : ["CSENSE012"]);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task SpreadInvocationPropagatesExceptions(bool scan)
    {
        using var test = Create(false, "=> [with(0), .. Entries()];", scan,
            "/// <exception cref=\"System.ArgumentException\">Invalid capacity.</exception>");
        test.AddDocument("""
            public partial class Factory
            {
                /// <summary>Reads entries.</summary>
                /// <returns>The available entries.</returns>
                /// <exception cref="System.NotSupportedException">Reading is unavailable.</exception>
                private static int[] Entries() => throw new System.NotSupportedException();
            }
            """);
        await test.VerifyAsync(scan ? ["CSENSE012"] : []);
    }

    [TestCase(false)]
    [TestCase(true)]
    public async Task ExplicitCallsRemainEquivalent(bool builder)
    {
        using var test = Create(builder, builder ? "=> Builder.Create(-1, [1]);" : "=> new Bag(-1) { 1 };");
        await test.VerifyAsync("CSENSE012");
    }

    private static CompilerTest Create(bool builder, string body, bool scan = true, string exceptionTag = "") => new(
        (builder ? Builder : Collection) + "\n" + $$"""
        /// <summary>Constructs collections.</summary>
        public partial class Factory
        {
            /// <summary>Creates entry storage.</summary>
            /// <returns>The new storage.</returns>
            {{exceptionTag}}
            public Bag Create() {{body}}
        }
        """, options: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
}
