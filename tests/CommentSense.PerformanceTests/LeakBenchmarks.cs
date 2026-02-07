using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using BenchmarkDotNet.Attributes;
using System.Globalization;

namespace CommentSense.PerformanceTests;

[MemoryDiagnoser]
public class LeakBenchmarks : BenchmarkBase
{
    private CSharpParseOptions _parseOptions = null!;
    private List<MetadataReference> _references = null!;

    public override void Setup()
    {
        base.Setup();
        _parseOptions = new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse);
        _references = [..GetMetadataReferences()];
    }

    protected override string GetSourceCode() => "";

    [Benchmark]
    public async Task SimulateLongSession()
    {
        // Simulate a developer editing code and triggering multiple analysis runs
        // If our ConditionalWeakTable or other caches have leaks, memory will grow here
        for (int i = 0; i < 200; i++)
        {
            var source = string.Create(CultureInfo.InvariantCulture, $$"""
                                                                        /// <summary> Test {{i}} </summary>
                                                                        public class C{{i}} { }
                                                                        """);
            var tree = CSharpSyntaxTree.ParseText(source, _parseOptions);
            var compilation = CSharpCompilation.Create($"LeakTest{i}", [tree], _references);

            // Create new options provider and options to stress the static OptionsCache
            var optionsProvider = new TestAnalyzerConfigOptionsProvider();
            var analyzerOptions = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, optionsProvider);

            var compilationWithAnalyzers = compilation.WithAnalyzers(Analyzers, analyzerOptions);
            await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
