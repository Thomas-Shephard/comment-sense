using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using System.Globalization;

namespace CommentSense.PerformanceTests;

[MemoryDiagnoser]
public class PathologicalBenchmarks : BenchmarkBase
{
    [Params(10)]
    public int DocSizeMultiplier { get; set; }

    public override void Setup()
    {
        base.Setup();
        // Force the O(N*M) Levenshtein calculation and deep scanning on every summary
        OptionsProvider.SetOption("comment_sense.similarity_threshold", "0.1");
        OptionsProvider.SetOption("comment_sense.scan_called_methods_for_exceptions", "true");
    }

    protected override string GetSourceCode()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("namespace Pathological;");
        sb.AppendLine("public class StressTest {");

        for (int m = 0; m < 10; m++)
        {
            // Generate a method with a massive amount of documentation
            sb.AppendLine("/// <summary>");
            for (int i = 0; i < DocSizeMultiplier * 100; i++)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"/// This is line {i} of a very long summary meant to stress the XML parser and the quality analyzers. ");
            }
            sb.AppendLine("/// </summary>");

            for (int i = 0; i < 50; i++)
            {
                sb.AppendLine(CultureInfo.InvariantCulture, $"/// <param name=\"p{i}\">Parameter {i} description that might mention p{i} multiple times.</param>");
            }

            sb.Append(CultureInfo.InvariantCulture, $"public void LargeMethod{m}(");
            sb.Append(string.Join(", ", Enumerable.Range(0, 50).Select(i => $"string p{i}")));
            sb.AppendLine(") { }");
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    [Benchmark]
    public async Task AnalyzePathologicalDocs()
    {
        await RunAnalysisAsync();
    }
}
