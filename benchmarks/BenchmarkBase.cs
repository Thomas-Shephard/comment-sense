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
        Compilation = CreateCompilation();
        ValidateCompilation(Compilation);

        Analyzers = [new CommentSenseAnalyzer()];

        OptionsProvider = new TestAnalyzerConfigOptionsProvider();
        Options = new AnalyzerOptions(ImmutableArray<AdditionalText>.Empty, OptionsProvider);
    }

    protected virtual Compilation CreateCompilation()
    {
        var source = GetSourceCode();
        var syntaxTrees = string.IsNullOrEmpty(source)
            ? GetSyntaxTrees()
            : [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions().WithDocumentationMode(DocumentationMode.Parse))];

        var references = GetMetadataReferences();

        return CSharpCompilation.Create("BenchmarkAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
    }

    protected static void ValidateCompilation(Compilation compilation)
    {
        var errors = compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length != 0)
            throw new InvalidOperationException(string.Join(Environment.NewLine, errors.Select(d => d.ToString())));
    }

    protected abstract string GetSourceCode();

    protected virtual IEnumerable<SyntaxTree> GetSyntaxTrees() => [];

    protected static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        return [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)];
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

    protected Task RunAnalysisAsync() => RunAnalysisAsync(Compilation, Options);

    protected async Task RunAnalysisAsync(Compilation compilation, AnalyzerOptions options)
    {
        var diagnostics = await compilation.WithAnalyzers(Analyzers, options).GetAnalyzerDiagnosticsAsync();
        foreach (var diagnostic in diagnostics)
        {
            if (diagnostic.Id is "AD0001" or "AD0002")
                throw new InvalidOperationException(diagnostic.ToString());
        }
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
