using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;

namespace CommentSense.TestHelpers;

public abstract class CommentSenseCodeFixTestBase<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : Microsoft.CodeAnalysis.CodeFixes.CodeFixProvider, new()
{
    protected static async Task VerifyCodeFixAsync(string source, string fixedSource, IDictionary<string, string>? configOptions = null, DocumentationMode documentationMode = DocumentationMode.Parse, IEnumerable<DiagnosticResult>? expectedDiagnostics = null)
    {
        var tester = new CSharpCodeFixTest<TAnalyzer, TCodeFix, NUnitVerifier>
        {
            TestCode = source.NormalizeLineEndings(),
            FixedCode = fixedSource.NormalizeLineEndings(),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        tester.ApplyCommonConfiguration(configOptions, documentationMode, expectedDiagnostics);

        await tester.RunAsync();
    }

    protected static async Task VerifyCodeFixTitleAsync(string source, string fixedSource, string expectedTitle, IDictionary<string, string>? configOptions = null)
    {
        var tester = new CSharpCodeFixTest<TAnalyzer, TCodeFix, NUnitVerifier>
        {
            TestCode = source.NormalizeLineEndings(),
            FixedCode = fixedSource.NormalizeLineEndings(),
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        tester.ApplyCommonConfiguration(configOptions, DocumentationMode.Parse, null);

        tester.CodeActionVerifier = (action, verifier) =>
        {
            verifier.Equal(expectedTitle, action.Title);
        };

        await tester.RunAsync();
    }
}
