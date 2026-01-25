using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.TestHelpers;

public abstract class CommentSenseAnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static async Task VerifyCSenseAsync(string source, bool expectDiagnostic = true, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors, IEnumerable<(string Id, ReportDiagnostic Severity)>? diagnosticOptions = null)
    {
        var tester = new CSharpAnalyzerTest<TAnalyzer, NUnitVerifier>
        {
            TestCode = source,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CompilerDiagnostics = compilerDiagnostics
        };

        if (diagnosticOptions != null)
        {
            foreach (var (id, severity) in diagnosticOptions)
            {
                tester.SolutionTransforms.Add((solution, projectId) =>
                {
                    var project = solution.GetProject(projectId);
                    var compilationOptions = project?.CompilationOptions;
                    compilationOptions = compilationOptions?.WithSpecificDiagnosticOptions(
                        compilationOptions.SpecificDiagnosticOptions.Add(id, severity));

                    if (compilationOptions is null)
                        throw new ArgumentException($"Compilation options must be specified for {id}.");

                    return solution.WithProjectCompilationOptions(projectId, compilationOptions);
                });
            }
        }

        if (expectDiagnostic && !source.Contains("{|") && !source.Contains("[|"))
            Assert.Fail("expectDiagnostic is true but test code contains no diagnostic markers {| |} or [| |].");

        if (!expectDiagnostic && (source.Contains("{|") || source.Contains("[|")))
            Assert.Fail("Test code contains diagnostic markers {| |} or [| |] but expectDiagnostic is false.");

        await tester.RunAsync();
    }
}
