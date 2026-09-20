using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CollectionExpressionExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [TestCase("[]", true, false, false)]
    [TestCase("[1, 2]", true, false, false)]
    [TestCase("new Bag { 1, 2 }", true, false, false)]
    [TestCase("[1, 2]", false, false, false)]
    [TestCase("[1, 2]", true, true, false)]
    [TestCase("[1, 2]", true, false, true)]
    public async Task ConstructorExceptions(string expression, bool scan, bool documented, bool caught)
    {
        var missing = scan && !documented && !caught;
        var body = caught
            ? $"try {{ return {expression}; }} catch (System.ArgumentException) {{ return null; }}"
            : $"return {expression};";
        var source = $$"""
            /// <summary>Stores entries.</summary>
            public class Bag : System.Collections.Generic.List<int>
            {
                /// <summary>Creates entry storage.</summary>
                /// <exception cref="System.ArgumentException">Storage cannot be created.</exception>
                public Bag() { }

                /// <summary>Creates entry storage with a capacity.</summary>
                /// <param name="capacity">The initial capacity.</param>
                /// <exception cref="System.InvalidOperationException">Storage is unavailable.</exception>
                public Bag(int capacity) { }
            }
            /// <summary>Creates collections.</summary>
            public class Factory
            {
                /// <summary>Creates entry storage.</summary>
                /// <returns>The storage.</returns>
                {{(documented ? "/// <exception cref=\"System.ArgumentException\">Storage cannot be created.</exception>" : "")}}
                public Bag {{(missing ? "{|CSENSE012:Create|}" : "Create")}}() { {{body}} }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: missing,
            referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
    }

    [TestCase("[]", true, false)]
    [TestCase("[1, 2]", true, false)]
    [TestCase("Builder.Create<int>([1, 2])", true, false)]
    [TestCase("[1, 2]", false, false)]
    [TestCase("[1, 2]", true, true)]
    public async Task BuilderExceptions(string expression, bool scan, bool documented)
    {
        var missing = scan && !documented;
        var source = $$"""
            /// <summary>Stores entries.</summary>
            /// <typeparam name="T">The entry type.</typeparam>
            [System.Runtime.CompilerServices.CollectionBuilder(typeof(Builder), "Create")]
            public class Bag<T> : System.Collections.Generic.List<T> { }
            /// <summary>Constructs collections.</summary>
            public static class Builder
            {
                /// <summary>Creates entry storage.</summary>
                /// <typeparam name="T">The entry type.</typeparam>
                /// <param name="items">The initial entries.</param>
                /// <returns>The storage.</returns>
                /// <exception cref="System.ArgumentException">An entry is invalid.</exception>
                public static Bag<T> Create<T>(System.ReadOnlySpan<T> items) => new Bag<T>();

                /// <summary>Creates entry storage from an array.</summary>
                /// <typeparam name="T">The entry type.</typeparam>
                /// <param name="items">The initial entries.</param>
                /// <returns>The storage.</returns>
                /// <exception cref="System.InvalidOperationException">Storage is unavailable.</exception>
                public static Bag<T> Create<T>(T[] items) => new Bag<T>();
            }
            /// <summary>Creates collections.</summary>
            public class Factory
            {
                /// <summary>Creates entry storage.</summary>
                /// <returns>The storage.</returns>
                {{(documented ? "/// <exception cref=\"System.ArgumentException\">An entry is invalid.</exception>" : "")}}
                public Bag<int> {{(missing ? "{|CSENSE012:Create|}" : "Create")}}() => {{expression}};
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: missing,
            referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
    }

    [TestCase("int[]")]
    [TestCase("System.ReadOnlySpan<int>")]
    [TestCase("System.Collections.Generic.IEnumerable<int>")]
    public async Task CollectionsWithoutConstructionMethods(string type)
    {
        var source = $$"""
            /// <summary>Creates collections.</summary>
            public class Factory
            {
                /// <summary>Creates entries.</summary>
                /// <returns>The entries.</returns>
                public {{type}} Create() => [1, 2];
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: false,
            referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" });
    }
}
