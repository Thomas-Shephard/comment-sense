using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class DocumentationModeTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task DisabledDocumentationParsingReportsDiagnostic()
    {
        const string testCode = """
            public class MyClass
            {
                public void Method() { }
            }
            """;

        var expected = new DiagnosticResult("CSENSE000", DiagnosticSeverity.Warning).WithNoLocation();

        await VerifyCSenseAsync(
            testCode,
            documentationMode: DocumentationMode.None,
            expectedDiagnostics: [expected]
        );
    }

    [Test]
    public async Task EnabledDocumentationParsingDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>
            /// Valid documentation.
            /// </summary>
            public class MyClass
            {
                /// <summary>
                /// Valid documentation.
                /// </summary>
                public void Method() { }
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            expectDiagnostic: false,
            documentationMode: DocumentationMode.Parse
        );
    }

    [Test]
    public async Task MixedDocumentationModeCorrectlyHandlesEachFile()
    {
        // File 1: Parse
        const string file1 = """
            /// <summary>Valid</summary>
            public class ValidClass
            {
                public void {|CSENSE001:MissingDocsMethod|}() { }
            }
            """;

        // File 2: None
        const string file2 = "public class DisabledClass { }";

        var tester = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpAnalyzerTest<CommentSenseAnalyzer, NUnitVerifier>
        {
            TestState =
            {
                Sources = { file1 }
            }
        };

        tester.SolutionTransforms.Add((solution, projectId) =>
        {
            var project1 = solution.GetProject(projectId);
            Assert.That(project1, Is.Not.Null);

            // Project 1 Parse
            var optionsParse = (project1.ParseOptions as CSharpParseOptions)?.WithDocumentationMode(DocumentationMode.Parse);
            Assert.That(optionsParse, Is.Not.Null);
            solution = solution.WithProjectParseOptions(projectId, optionsParse);

            // Create Project 2 with DocumentationMode.None
            var project2Id = ProjectId.CreateNewId();
            solution = solution.AddProject(project2Id, "Project2", "Project2", LanguageNames.CSharp);
            solution = solution.AddMetadataReferences(project2Id, project1.MetadataReferences);
            solution = solution.WithProjectParseOptions(project2Id, optionsParse.WithDocumentationMode(DocumentationMode.None));

            // Add file2 to Project 2
            solution = solution.AddDocument(DocumentId.CreateNewId(project2Id), "DisabledClass.cs", file2);

            // Add reference from Project 1 to Project 2
            solution = solution.AddProjectReferences(projectId, [new ProjectReference(project2Id)]);

            return solution;
        });

        tester.CompilerDiagnostics = CompilerDiagnostics.None;
        tester.ExpectedDiagnostics.Add(new DiagnosticResult("CSENSE000", DiagnosticSeverity.Warning).WithNoLocation());

        await tester.RunAsync();
    }

    [Test]
    public async Task MixedDocumentationModeOnlyAnalyzesEnabledTrees()
    {
        // 1. Create SyntaxTrees with mixed DocumentationMode
        const string codeEnabled = """
            public class EnabledClass
            {
                public void Method() { }
            }
            """;

        const string codeDisabled = """
            public class DisabledClass
            {
                public void Method() { }
            }
            """;

        var treeEnabled = CSharpSyntaxTree.ParseText(codeEnabled, new CSharpParseOptions(documentationMode: DocumentationMode.Parse));
        var treeDisabled = CSharpSyntaxTree.ParseText(codeDisabled, new CSharpParseOptions(documentationMode: DocumentationMode.None));

        // 2. Create Compilation
        var compilation = CSharpCompilation.Create("MixedModeTest")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(treeEnabled, treeDisabled);

        // 3. Create Analyzer Driver
        var compilationWithAnalyzers = compilation.WithAnalyzers([new CommentSenseAnalyzer()]);

        // 4. Get Diagnostics
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // 5. Verify Results

        // Should have CSENSE000 (Project has disabled docs)
        var globalWarnings = diagnostics.Where(d => d.Id == "CSENSE000").ToList();
        Assert.That(globalWarnings, Has.Count.EqualTo(1), "Should report CSENSE000 for mixed/disabled documentation mode.");

        // Should have CSENSE001 for EnabledClass (Missing Documentation)
        // because it is in a Parse-mode tree, so AnalyzeSymbol should run and report missing docs.
        var enabledWarnings = diagnostics.Where(d => d.Id == "CSENSE001" && d.Location.SourceTree == treeEnabled).ToList();
        Assert.That(enabledWarnings, Is.Not.Empty, "Should report missing documentation for class in enabled tree.");

        // Should NOT have CSENSE001 for DisabledClass
        // because it is in a None-mode tree, so AnalyzeSymbol should return early.
        var disabledWarnings = diagnostics.Where(d => d.Id == "CSENSE001" && d.Location.SourceTree == treeDisabled).ToList();
        Assert.That(disabledWarnings, Is.Empty, "Should NOT report missing documentation for class in disabled tree.");
    }

    [Test]
    public async Task PartialClassWithMixedDocumentationModeIsAnalyzed()
    {
        // One part in a disabled file
        const string codeDisabled = "public partial class PartialClass { }";

        // One part in an enabled file
        const string codeEnabled = "public partial class {|CSENSE001:PartialClass|} { }";

        var treeDisabled = CSharpSyntaxTree.ParseText(codeDisabled, new CSharpParseOptions(documentationMode: DocumentationMode.None));
        var treeEnabled = CSharpSyntaxTree.ParseText(codeEnabled, new CSharpParseOptions(documentationMode: DocumentationMode.Parse));

        var compilation = CSharpCompilation.Create("PartialMixedTest")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location))
            .AddSyntaxTrees(treeDisabled, treeEnabled); // Add disabled FIRST

        var compilationWithAnalyzers = compilation.WithAnalyzers([new CommentSenseAnalyzer()]);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync();

        // Should report CSENSE001 because one part is in an enabled tree.
        var warning = diagnostics.FirstOrDefault(d => d.Id == "CSENSE001");
        Assert.That(warning, Is.Not.Null, "Should report missing documentation for partial class even if primary location is disabled.");
    }
}
