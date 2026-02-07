using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using BenchmarkDotNet.Attributes;

namespace CommentSense.PerformanceTests;

[MemoryDiagnoser]
public class DogfoodBenchmarks : BenchmarkBase
{
    protected override IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        var rootPath = GetSourceRoot();
        var files = Directory.GetFiles(rootPath, "*.cs", SearchOption.AllDirectories);

        return files.Select(f =>
            CSharpSyntaxTree.ParseText(File.ReadAllText(f), new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse))
        );
    }

    public override void Setup()
    {
        base.Setup();

        // Enable heavy features for dogfooding
        OptionsProvider.SetOption("comment_sense.scan_called_methods_for_exceptions", "true");
        OptionsProvider.SetOption("comment_sense.ghost_references.mode", "strict");
        OptionsProvider.SetOption("comment_sense.similarity_threshold", "0.8");
    }

    protected override string GetSourceCode() => ""; // Not used for dogfood

    [Benchmark]
    public async Task AnalyzeProject()
    {
        await RunAnalysisAsync();
    }
}
