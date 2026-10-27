using System.Collections.Immutable;
using CommentSense.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

internal sealed class CompilerTest : IDisposable
{
    private static readonly ImmutableArray<MetadataReference> References = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")
        ?? throw new InvalidOperationException("Missing runtime assemblies."))
        .Split(Path.PathSeparator)
        .Where(path => Path.GetDirectoryName(path) == Path.GetDirectoryName(typeof(object).Assembly.Location))
        .Select(path => MetadataReference.CreateFromFile(path))
        .ToImmutableArray<MetadataReference>();

    private readonly AdhocWorkspace workspace = new();

    public CompilerTest(string source, string language = "15", bool updatedMemorySafety = false,
        IReadOnlyDictionary<string, string>? options = null, IReadOnlyDictionary<string, ReportDiagnostic>? diagnosticOptions = null)
    {
        Assert.That(LanguageVersionFacts.TryParse(language, out var version), Is.True, "The SDK must support the requested C# version.");
        var parseOptions = new CSharpParseOptions(version, documentationMode: DocumentationMode.Diagnose);
        if (updatedMemorySafety)
            parseOptions = parseOptions.WithFeatures([new("updated-memory-safety-rules", "true")]);

        var configuration = new Dictionary<string, string>
        {
            ["end_of_line"] = Environment.NewLine == "\r\n" ? "crlf" : "lf",
            ["comment_sense.scan_called_methods_for_exceptions"] = "true"
        };
        if (options != null)
        {
            foreach (var pair in options)
                configuration[pair.Key] = pair.Value;
        }

        var severities = new Dictionary<string, ReportDiagnostic> { ["CSENSE014"] = ReportDiagnostic.Warn };
        if (diagnosticOptions != null)
        {
            foreach (var pair in diagnosticOptions)
                severities[pair.Key] = pair.Value;
        }
        var project = workspace.AddProject("Consumer", LanguageNames.CSharp)
            .WithMetadataReferences(References)
            .WithParseOptions(parseOptions)
            .WithCompilationOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary,
                allowUnsafe: true, specificDiagnosticOptions: severities));
        var configText = "is_global = true\n" + string.Join("\n", configuration.Select(pair => $"{pair.Key} = {pair.Value}"));
        project = project.AddAnalyzerConfigDocument(".globalconfig", SourceText.From(configText),
            filePath: Path.Combine(Path.GetTempPath(), ".globalconfig")).Project;
        Document = project.AddDocument("Consumer.cs", SourceText.From(Normalize(source)),
            filePath: Path.Combine(Path.GetTempPath(), "Consumer.cs"));
    }

    public Document Document { get; private set; }

    public void AddDocument(string source)
    {
        var next = Document.Project.AddDocument($"Consumer{Document.Project.DocumentIds.Count}.cs", SourceText.From(Normalize(source)));
        Document = next.Project.GetDocument(Document.Id) ?? throw new InvalidOperationException("Missing original document.");
    }

    public async Task<ImmutableArray<Diagnostic>> AnalyzeAsync(bool suppress = false, bool requireValidSource = true)
    {
        var compilation = await Document.Project.GetCompilationAsync() ?? throw new InvalidOperationException("Missing compilation.");
        if (requireValidSource)
            Assert.That(compilation.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty);

        ImmutableArray<DiagnosticAnalyzer> analyzers = suppress
            ? [new CommentSenseAnalyzer(), new CommentSenseSuppressor()]
            : [new CommentSenseAnalyzer()];
        var analysis = compilation.WithAnalyzers(analyzers, new CompilationWithAnalyzersOptions(
            Document.Project.AnalyzerOptions, null, true, false, true));
        var diagnostics = suppress ? await analysis.GetAllDiagnosticsAsync() : await analysis.GetAnalyzerDiagnosticsAsync();
        Assert.That(diagnostics.Where(d => d.Id is "AD0001" or "AD0002" or "CS8032" or "CS9057"), Is.Empty);
        return diagnostics;
    }

    public async Task VerifyAsync(params string[] expectedIds)
    {
        var diagnostics = await AnalyzeAsync();
        Assert.That(diagnostics.Select(d => d.Id), Is.EquivalentTo(expectedIds), string.Join(Environment.NewLine, diagnostics));
    }

    public async Task FixAsync(CodeFixProvider provider, string diagnosticId, FixAllScope? scope = null)
    {
        var diagnostics = await AnalyzeAsync();
        var diagnostic = diagnostics.First(d => d.Id == diagnosticId);
        var document = Document.Project.GetDocument(diagnostic.Location.SourceTree) ?? throw new InvalidOperationException("Missing diagnostic document.");
        var actions = new List<CodeAction>();
        await provider.RegisterCodeFixesAsync(new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None));
        Assert.That(actions, Is.Not.Empty);
        var action = actions[0];
        if (scope is { } fixAllScope)
        {
            var fixer = provider.GetFixAllProvider() ?? throw new InvalidOperationException("Missing Fix All provider.");
            action = await fixer.GetFixAsync(new FixAllContext(document, provider, fixAllScope, action.EquivalenceKey,
                [diagnosticId], new FixDiagnostics(diagnostics), CancellationToken.None))
                ?? throw new InvalidOperationException("Missing Fix All action.");
        }

        var operations = await action.GetOperationsAsync(CancellationToken.None);
        var solution = operations.OfType<ApplyChangesOperation>().Single().ChangedSolution;
        Document = solution.GetDocument(Document.Id) ?? throw new InvalidOperationException("Missing fixed document.");
    }

    public async Task VerifySourceAsync(string expected)
    {
        Assert.That((await Document.GetTextAsync()).ToString(), Is.EqualTo(Normalize(expected)));
    }

    public async Task VerifyFixAllUnchangedAsync(CodeFixProvider provider, string diagnosticId, FixAllScope scope)
    {
        var diagnostics = await AnalyzeAsync();
        Assert.That(diagnostics.Any(d => d.Id == diagnosticId), Is.False);
        var fixer = provider.GetFixAllProvider() ?? throw new InvalidOperationException("Missing Fix All provider.");
        var action = await fixer.GetFixAsync(new FixAllContext(Document, provider, scope, null,
            [diagnosticId], new FixDiagnostics(diagnostics), CancellationToken.None));
        if (action is null)
            return;
        var operations = await action.GetOperationsAsync(CancellationToken.None);
        foreach (var operation in operations.OfType<ApplyChangesOperation>())
        {
            foreach (var document in Document.Project.Documents)
            {
                var changed = operation.ChangedSolution.GetDocument(document.Id) ?? throw new InvalidOperationException("Missing document.");
                Assert.That((await changed.GetTextAsync()).ToString(), Is.EqualTo((await document.GetTextAsync()).ToString()));
            }
        }
    }

    public static string Normalize(string source) => source.ReplaceLineEndings();

    public void Dispose() => workspace.Dispose();

    private sealed class FixDiagnostics(ImmutableArray<Diagnostic> diagnostics) : FixAllContext.DiagnosticProvider
    {
        public override async Task<IEnumerable<Diagnostic>> GetDocumentDiagnosticsAsync(Document document, CancellationToken cancellationToken)
        {
            var tree = await document.GetSyntaxTreeAsync(cancellationToken);
            return diagnostics.Where(d => d.Location.SourceTree == tree);
        }

        public override Task<IEnumerable<Diagnostic>> GetProjectDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>([]);

        public override Task<IEnumerable<Diagnostic>> GetAllDiagnosticsAsync(Project project, CancellationToken cancellationToken) =>
            Task.FromResult<IEnumerable<Diagnostic>>(diagnostics);
    }
}
