using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;

namespace CommentSense.TestHelpers;

public static class CommentSenseTestExtensions
{
    private static readonly string[] EditorConfigHeader = ["is_global = true", "end_of_line = crlf"];

    public static string NormalizeLineEndings(this string text) => text.Replace("\r\n", "\n").Replace("\n", "\r\n");

    public static void ApplyCommonConfiguration<TVerifier>(
        this AnalyzerTest<TVerifier> test,
        IDictionary<string, string>? configOptions,
        DocumentationMode documentationMode,
        IEnumerable<DiagnosticResult>? expectedDiagnostics)
        where TVerifier : IVerifier, new()
    {
        test.SolutionTransforms.Add((solution, projectId) =>
        {
            var project = solution.GetProject(projectId);
            if (project == null) return solution;

            if (project.ParseOptions is CSharpParseOptions parseOptions)
            {
                solution = solution.WithProjectParseOptions(projectId, parseOptions.WithDocumentationMode(documentationMode));
            }

            return solution;
        });

        var options = EditorConfigHeader.Concat(configOptions?.Select(kv => $"{kv.Key} = {kv.Value}") ?? []);
        var configText = string.Join(Environment.NewLine, options);
        test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", configText));

        if (expectedDiagnostics != null)
        {
            test.ExpectedDiagnostics.AddRange(expectedDiagnostics);
        }
    }
}
