using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ExtensionDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [TestCase("(int[] values)", "extension(int[])")]
    [TestCase("<T>(T[] values)", "extension<T>(T[])")]
    [TestCase("(string)", "extension(string)")]
    public async Task MissingBlockDocumentationUsesReadableName(string declaration, string displayName)
    {
        var source = $$"""
            /// <summary>Provides sequence extensions.</summary>
            public static class Extensions
            {
                {|#0:extension|}{{declaration}}
                {
                    /// <summary>Gets the supported count.</summary>
                    /// <value>The supported count.</value>
                    public static int Count => 0;
                }
            }
            """;
        var expected = new DiagnosticResult(CommentSenseDiagnosticIds.MissingDocumentationId, DiagnosticSeverity.Warning)
            .WithLocation(0).WithArguments(displayName);

        await VerifyCSenseAsync(source, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task ReceiverAndTypeParameterDocumentationBelongsToBlock([Values] bool documented)
    {
        var source = $$"""
            /// <summary>Provides sequence extensions.</summary>
            public static class Extensions
            {
                /// <summary>Extends sequences.</summary>
                {{(documented ? "/// <typeparam name=\"T\">The element type.</typeparam>\n    /// <param name=\"values\">The sequence to access.</param>" : "")}}
                extension<{{(documented ? "T" : "{|CSENSE004:T|}")}}> (T[] {{(documented ? "values" : "{|CSENSE002:values|}")}})
                {
                    /// <summary>Gets the first element of <paramref name="values"/>.</summary>
                    /// <value>The first <typeparamref name="T"/> element.</value>
                    public T First => values[0];

                    /// <summary>Returns the requested element.</summary>
                    /// <param name="index">The position to read.</param>
                    /// <returns>The selected element.</returns>
                    public T At(int index) => values[index];
                }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !documented);
    }

    [Test]
    public async Task UnnamedReceiverNeedsNoParameterDocumentation()
    {
        const string source = """
            /// <summary>Provides text extensions.</summary>
            public static class Extensions
            {
                /// <summary>Extends text values.</summary>
                extension(string)
                {
                    /// <summary>Gets the empty text.</summary>
                    /// <value>The empty text.</value>
                    public static string Empty => string.Empty;
                }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: false);
    }

    [Test]
    public async Task ReceiverTagsAreValidatedOnBlock()
    {
        const string source = """
            /// <summary>Provides text extensions.</summary>
            public static class Extensions
            {
                /// <summary>Extends text values.</summary>
                /// <param name="text">The text to read.</param>
                /// {|CSENSE009:<param name="text">The repeated receiver.</param>|}
                /// {|CSENSE003:<param name="other">An unknown receiver.</param>|}
                extension(string text)
                {
                    /// <summary>Gets the character count.</summary>
                    /// <value>The character count.</value>
                    public int Count => text.Length;
                }
            }
            """;

        await VerifyCSenseAsync(source);
    }

    [Test]
    public async Task MembersStillRequireTheirOwnDocumentation()
    {
        const string source = """
            /// <summary>Provides text extensions.</summary>
            public static class Extensions
            {
                /// <summary>Extends text values.</summary>
                /// <param name="text">The text to read.</param>
                extension(string text)
                {
                    public int {|CSENSE001:Count|} => text.Length;

                    /// <summary>Returns the requested character.</summary>
                    /// <returns>The selected character.</returns>
                    public char At(int {|CSENSE002:index|}) => text[index];
                }
            }
            """;

        await VerifyCSenseAsync(source);
    }
}
