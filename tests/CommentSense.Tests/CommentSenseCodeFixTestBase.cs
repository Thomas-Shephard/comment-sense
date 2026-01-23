using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using CommentSense.Analyzers;
using CommentSense.CodeFixes;

namespace CommentSense.Tests;

public abstract class CommentSenseCodeFixTestBase : CommentSenseTestBase
{
    protected static async Task VerifyCodeFixAsync(string originalSource, string fixedSource)
    {
        var tester = new CSharpCodeFixTest<CommentSenseAnalyzer, CommentSenseCodeFixProvider, NUnitVerifier>
        {
            TestCode = originalSource,
            FixedCode = fixedSource,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        await tester.RunAsync();
    }
}
