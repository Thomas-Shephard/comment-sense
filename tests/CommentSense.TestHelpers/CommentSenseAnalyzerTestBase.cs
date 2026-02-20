using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.TestHelpers;

public abstract class CommentSenseAnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static async Task VerifyCSenseAsync(string source, bool expectDiagnostic = true, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors, IEnumerable<(string Id, ReportDiagnostic Severity)>? diagnosticOptions = null, IDictionary<string, string>? configOptions = null, DocumentationMode documentationMode = DocumentationMode.Parse, IEnumerable<DiagnosticResult>? expectedDiagnostics = null, ReferenceAssemblies? referenceAssemblies = null, Func<Solution, ProjectId, Solution>? solutionTransform = null, IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
    {
        var tester = new CustomAnalyzerTest(additionalAnalyzers)
        {
            TestCode = source.NormalizeLineEndings(),
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CompilerDiagnostics = compilerDiagnostics
        };

        if (referenceAssemblies != null)
            tester.TestState.ReferenceAssemblies = referenceAssemblies;

        if (solutionTransform != null)
            tester.SolutionTransforms.Add(solutionTransform);

        tester.ApplyCommonConfiguration(configOptions, documentationMode, expectedDiagnostics);

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

        if (expectDiagnostic && !source.Contains("{|") && expectedDiagnostics == null)
            Assert.Fail("expectDiagnostic is true but test code contains no diagnostic markers {| |} and no expectedDiagnostics were provided.");

        if (!expectDiagnostic && source.Contains("{|"))
            Assert.Fail("Test code contains diagnostic markers {| |} but expectDiagnostic is false.");

        await tester.RunAsync();
    }

    private sealed class CustomAnalyzerTest(IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers) : CSharpAnalyzerTest<TAnalyzer, NUnitVerifier>
    {
        protected override IEnumerable<DiagnosticAnalyzer> GetDiagnosticAnalyzers()
        {
            foreach (var analyzer in base.GetDiagnosticAnalyzers())
                yield return analyzer;

            if (additionalAnalyzers == null)
                yield break;

            foreach (var analyzer in additionalAnalyzers)
                yield return analyzer;
        }
    }
}
