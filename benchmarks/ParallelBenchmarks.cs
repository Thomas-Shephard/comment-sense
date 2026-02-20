using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using BenchmarkDotNet.Attributes;
using System.Globalization;

namespace CommentSense.PerformanceTests;

[MemoryDiagnoser]
#if WINDOWS
[ThreadingDiagnoser]
#endif
public class ParallelBenchmarks : BenchmarkBase
{
    [Params(100)]
    public int FileCount { get; set; }

    protected override IEnumerable<SyntaxTree> GetSyntaxTrees()
    {
        var syntaxTrees = new List<SyntaxTree>();
        for (int i = 0; i < FileCount; i++)
        {
            var source = string.Create(CultureInfo.InvariantCulture, $$"""
                using System;
                namespace ParallelTest.N{{i}};
                /// <summary> Class {{i}} </summary>
                public class Class{{i}}
                {
                    /// <summary> Method {{i}} </summary>
                    /// <exception cref="ArgumentNullException">Thrown when arg is null.</exception>
                    public void Method{{i}}(string arg)
                    {
                        if (arg == null) throw new ArgumentNullException(nameof(arg));
                    }
                }
                """);
            syntaxTrees.Add(CSharpSyntaxTree.ParseText(source, new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse)));
        }
        return syntaxTrees;
    }

    protected override string GetSourceCode() => "";

    [Benchmark]
    public async Task ConcurrentAnalysis()
    {
        // By default, Roslyn parallelizes analysis because we called context.EnableConcurrentExecution()
        await RunAnalysisAsync();
    }
}
