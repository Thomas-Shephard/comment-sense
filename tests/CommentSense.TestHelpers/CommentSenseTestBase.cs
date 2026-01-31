using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.TestHelpers;

public abstract class CommentSenseAnalyzerTestBase<TAnalyzer>
    where TAnalyzer : DiagnosticAnalyzer, new()
{
    protected static async Task VerifyCSenseAsync(string source, bool expectDiagnostic = true, CompilerDiagnostics compilerDiagnostics = CompilerDiagnostics.Errors, IEnumerable<(string Id, ReportDiagnostic Severity)>? diagnosticOptions = null, IDictionary<string, string>? configOptions = null, DocumentationMode documentationMode = DocumentationMode.Parse, IEnumerable<DiagnosticResult>? expectedDiagnostics = null, ReferenceAssemblies? referenceAssemblies = null, Func<Solution, ProjectId, Solution>? solutionTransform = null)
    {
        var tester = new CSharpAnalyzerTest<TAnalyzer, NUnitVerifier>
        {
            TestCode = source,
            MarkupOptions = MarkupOptions.UseFirstDescriptor,
            CompilerDiagnostics = compilerDiagnostics
        };

        if (referenceAssemblies != null)
            tester.TestState.ReferenceAssemblies = referenceAssemblies;

        if (solutionTransform != null)
            tester.SolutionTransforms.Add(solutionTransform);

        tester.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            if (project == null) return solution;

            if (project.ParseOptions is Microsoft.CodeAnalysis.CSharp.CSharpParseOptions parseOptions)
            {
                return solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(documentationMode));
            }

            return solution;
        });

        if (configOptions != null)
        {
            var configText = "is_global = true\n" + string.Join("\n", configOptions.Select(kv => $"{kv.Key} = {kv.Value}"));
            tester.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", configText));
        }

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

        if (expectedDiagnostics != null)
        {
            tester.ExpectedDiagnostics.AddRange(expectedDiagnostics);
        }

        if (expectDiagnostic && !source.Contains("{|") && expectedDiagnostics == null)
            Assert.Fail("expectDiagnostic is true but test code contains no diagnostic markers {| |} and no expectedDiagnostics were provided.");

        if (!expectDiagnostic && source.Contains("{|"))
            Assert.Fail("Test code contains diagnostic markers {| |} but expectDiagnostic is false.");

        await tester.RunAsync();
    }
}
