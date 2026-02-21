using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using BenchmarkDotNet.Attributes;
using CommentSense.Analyzers;

namespace CommentSense.PerformanceTests;

[SuppressMessage("ReSharper", "NullableWarningSuppressionIsUsed")]
[MemoryDiagnoser]
public abstract class BenchmarkBase
{
    private Compilation Compilation { get; set; } = null!;
    protected ImmutableArray<DiagnosticAnalyzer> Analyzers { get; private set; }
    private AnalyzerOptions Options { get; set; } = null!;
    protected TestAnalyzerConfigOptionsProvider OptionsProvider { get; private set; } = null!;

    [GlobalSetup]
    public virtual void Setup()
    {
        var source = GetSourceCode();
        var syntaxTrees = string.IsNullOrEmpty(source)
            ? GetSyntaxTrees()
            : [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse))];

        var references = GetMetadataReferences();

        Compilation = CSharpCompilation.Create("BenchmarkAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        Analyzers = [new CommentSenseAnalyzer()];

        OptionsProvider = new TestAnalyzerConfigOptionsProvider();
        Options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, OptionsProvider);
    }

    protected abstract string GetSourceCode();

    protected virtual IEnumerable<SyntaxTree> GetSyntaxTrees() => [];

    protected static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        return [.. AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
            .Select<System.Reflection.Assembly, MetadataReference>(a => MetadataReference.CreateFromFile(a.Location))];
    }

    protected static string GetSourceRoot()
    {
        var envRoot = Environment.GetEnvironmentVariable("COMMENT_SENSE_SOURCE_ROOT");
        if (!string.IsNullOrEmpty(envRoot))
            return envRoot;

        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current != null && !File.Exists(Path.Combine(current.FullName, "CommentSense.slnx")))
        {
            current = current.Parent;
        }

        return Path.Combine(current?.FullName ?? throw new InvalidOperationException("Could not find solution root"), "src");
    }

    protected async Task RunAnalysisAsync()
    {
        var compilationWithAnalyzers = Compilation.WithAnalyzers(Analyzers, Options);
        await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();
    }

    protected sealed class TestAnalyzerConfigOptionsProvider : AnalyzerConfigOptionsProvider
    {
        public override AnalyzerConfigOptions GlobalOptions { get; } = new TestAnalyzerConfigOptions();
        public override AnalyzerConfigOptions GetOptions(SyntaxTree tree) => GlobalOptions;
        public override AnalyzerConfigOptions GetOptions(AdditionalText textFile) => GlobalOptions;

        public void SetOption(string key, string value) => ((TestAnalyzerConfigOptions)GlobalOptions).Set(key, value);
    }

    private sealed class TestAnalyzerConfigOptions : AnalyzerConfigOptions
    {
        private readonly Dictionary<string, string> _options = [];
        public void Set(string key, string value) => _options[key] = value;
        public override bool TryGetValue(string key, [NotNullWhen(true)] out string? value) => _options.TryGetValue(key, out value);
    }
}
