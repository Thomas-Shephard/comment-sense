using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.TestHelpers;

public abstract class CommentSenseAnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static async Task VerifyCSenseAsync(string source, bool expectDiagnostic = true)
    {
        var tester = new CSharpAnalyzerTest<TAnalyzer, NUnitVerifier>
        {
            TestCode = source,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        if (expectDiagnostic && !source.Contains("{|") && !source.Contains("[|"))
            Assert.Fail("expectDiagnostic is true but test code contains no diagnostic markers {| |} or [| |].");

        if (!expectDiagnostic && (source.Contains("{|") || source.Contains("[|")))
            Assert.Fail("Test code contains diagnostic markers {| |} or [| |] but expectDiagnostic is false.");

        await tester.RunAsync();
    }
}
