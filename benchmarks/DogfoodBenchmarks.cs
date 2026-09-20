using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.MSBuild;
using BenchmarkDotNet.Attributes;

namespace CommentSense.PerformanceTests;

public class DogfoodBenchmarks : BenchmarkBase
{
    [Params("CommentSense.Core", "CommentSense.Analyzers", "CommentSense.CodeFixes")]
    public string ProjectName { get; set; } = "CommentSense.Analyzers";

    protected override Compilation CreateCompilation()
    {
        using var workspace = MSBuildWorkspace.Create(new Dictionary<string, string> { ["Configuration"] = "Release" });
        var projectPath = Path.Combine(GetSourceRoot(), ProjectName, ProjectName + ".csproj");
        var project = workspace.OpenProjectAsync(projectPath).GetAwaiter().GetResult();
        var compilation = project.GetCompilationAsync().GetAwaiter().GetResult()
            ?? throw new InvalidOperationException($"No compilation for {projectPath}.");
        var failures = workspace.Diagnostics.Where(d => d.Kind == WorkspaceDiagnosticKind.Failure).ToArray();
        if (failures.Length != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, failures.Select(d => d.Message)));
        return compilation;
    }

    public override void Setup()
    {
        base.Setup();

        OptionsProvider.SetOption("comment_sense.scan_called_methods_for_exceptions", "true");
        OptionsProvider.SetOption("comment_sense.ghost_references.mode", "strict");
        OptionsProvider.SetOption("comment_sense.similarity_threshold", "0.8");
    }

    protected override string GetSourceCode() => "";

    [Benchmark]
    public async Task AnalyzeProject()
    {
        await RunAnalysisAsync();
    }
}
