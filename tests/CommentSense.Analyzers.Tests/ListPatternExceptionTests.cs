using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ListPatternExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [TestCase("=> values is [1];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [];", true, "InvalidOperationException")]
    [TestCase("=> values is [_];", true, "InvalidOperationException")]
    [TestCase("=> values is [_ and _];", true, "InvalidOperationException")]
    [TestCase("=> values is [not not _];", true, "InvalidOperationException")]
    [TestCase("=> values is [1 or 2];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [_ and 1];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [var _];", true, "InvalidOperationException")]
    [TestCase("=> values is [var item];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [int item];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [1];", true, "ArgumentException,InvalidOperationException", "Count")]
    [TestCase("=> values is [..];", true, "")]
    [TestCase("=> values is [.. _];", true, "")]
    [TestCase("=> values is [.. var _];", true, "")]
    [TestCase("=> values is [.. var rest];", true, "InvalidOperationException,NotSupportedException")]
    [TestCase("=> values is [1, ..];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [1, .. _];", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [1, .. var rest];", true, "ArgumentException,InvalidOperationException,NotSupportedException")]
    [TestCase("=> values switch { [1] => true, _ => false };", true, "ArgumentException,InvalidOperationException")]
    [TestCase("{ switch (values) { case [1]: return true; default: return false; } }", true, "ArgumentException,InvalidOperationException")]
    [TestCase("=> values is [1];", false, "")]
    [TestCase("{ try { return values is [1]; } catch (System.Exception) { return false; } }", true, "")]
    [TestCase("{ System.Func<bool> match = () => values is [1]; return false; }", true, "")]
    public async Task ReportsSelectedListPatternMembers(string body, bool scan, string exceptions, string lengthProperty = "Length")
    {
        var method = exceptions.Length == 0 ? "Match" : "{|#0:Match|}";
        var source = $$"""
            /// <summary>Stores values.</summary>
            public class Storage
            {
                /// <summary>Gets the length.</summary>
                /// <value>The length.</value>
                /// <exception cref="System.InvalidOperationException">Storage is unavailable.</exception>
                public int {{lengthProperty}} => throw new System.InvalidOperationException();
                /// <summary>Gets a value.</summary>
                /// <param name="index">The position.</param>
                /// <value>The stored value.</value>
                /// <exception cref="System.ArgumentException">Invalid position.</exception>
                public int this[int index] => throw new System.ArgumentException();
                /// <summary>Gets a slice.</summary>
                /// <param name="start">The first position.</param>
                /// <param name="length">The number of values.</param>
                /// <returns>The slice.</returns>
                /// <exception cref="System.NotSupportedException">Slicing is unsupported.</exception>
                public Storage Slice(int start, int length) => throw new System.NotSupportedException();
            }
            /// <summary>Matches storage.</summary>
            public class Consumer
            {
                /// <summary>Matches stored values.</summary>
                /// <param name="values">The storage.</param>
                /// <returns>Whether the values match.</returns>
                public bool {{method}}(Storage values) {{body}}
            }
            """;
        var expected = exceptions.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(exception => new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule).WithLocation(0).WithArguments(exception));
        await VerifyCSenseAsync(source, expectDiagnostic: exceptions.Length != 0, expectedDiagnostics: expected,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ArrayListPatternDoesNotRequireExceptionDocumentation()
    {
        const string source = """
            /// <summary>Matches arrays.</summary>
            public class Consumer
            {
                /// <summary>Matches the first value.</summary>
                /// <param name="values">The array.</param>
                /// <returns>Whether the first value matches.</returns>
                public bool Match(int[] values) => values is [1, .. var rest];
            }
            """;
        await VerifyCSenseAsync(source, expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }
}
