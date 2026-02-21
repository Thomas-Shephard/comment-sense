using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using BenchmarkDotNet.Attributes;
using System.Globalization;

namespace CommentSense.PerformanceTests;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
public class LeakBenchmarks : BenchmarkBase
{
    private CSharpParseOptions _parseOptions = null!;
    private List<MetadataReference> _references = null!;

    public override void Setup()
    {
        base.Setup();

        _parseOptions = new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse);
        _references = [.. GetMetadataReferences()];
    }

    protected override string GetSourceCode() => "";

    [Benchmark]
    public async Task SimulateLongSession()
    {
        for (int i = 0; i < 50; i++)
        {
            var source = string.Create(CultureInfo.InvariantCulture, $$"""
                                                                        /// <summary> Test {{i}} </summary>
                                                                        public class C{{i}} { }
                                                                        """);
            var tree = CSharpSyntaxTree.ParseText(source, _parseOptions);
            var compilation = CSharpCompilation.Create($"LeakTest{i}", [tree], _references);

            var optionsProvider = new TestAnalyzerConfigOptionsProvider();
            var analyzerOptions = new AnalyzerOptions([], optionsProvider);

            var compilationWithAnalyzers = compilation.WithAnalyzers(Analyzers, analyzerOptions);
            await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
        }
    }
}
