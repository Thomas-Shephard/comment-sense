using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;

namespace CommentSense.PerformanceTests;

[MemoryDiagnoser]
public class AnalyzerBenchmarks : BenchmarkBase
{
    [Params(100)]
    public int MethodCount { get; set; }

    [Params(true)]
    public bool ScanCalledMethods { get; set; }

    [Params("strict")]
    public string GhostReferenceMode { get; set; } = "strict";

    [Params(0.8)]
    public double SimilarityThreshold { get; set; }

    protected override string GetSourceCode()
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("namespace BenchmarkNamespace;");
        sb.AppendLine("public class BenchmarkClass {");

        for (int i = 0; i < MethodCount; i++)
        {
            // Mixing in ghost references and potential similarity triggers
            sb.AppendLine(CultureInfo.InvariantCulture, $$"""
                /// <summary> This is a summary for Method {{i}} that might repeat Method{{i}} name. </summary>
                /// <param name="arg">The argument named arg.</param>
                /// <exception cref="ArgumentNullException">Thrown when arg is null.</exception>
                public void Method{{i}}(string arg)
                {
                    if (arg == null) throw new ArgumentNullException(nameof(arg));
                    InternalMethod();
                }
                """);
        }

        sb.AppendLine("""
            private void InternalMethod()
            {
                throw new InvalidOperationException("Something went wrong");
            }
            """);

        sb.AppendLine("}");
        return sb.ToString();
    }

    public override void Setup()
    {
        base.Setup();

        OptionsProvider.SetOption("comment_sense.scan_called_methods_for_exceptions", ScanCalledMethods.ToString().ToLowerInvariant());
        OptionsProvider.SetOption("comment_sense.ghost_references.mode", GhostReferenceMode);
        OptionsProvider.SetOption("comment_sense.similarity_threshold", SimilarityThreshold.ToString(CultureInfo.InvariantCulture));
    }

    [Benchmark]
    public async Task FullAnalysis()
    {
        await RunAnalysisAsync();
    }
}
