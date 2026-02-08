using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using CommentSense.TestHelpers;
using NUnit.Framework;
using System.Collections.Immutable;

namespace CommentSense.Analyzers.Tests;

public class SuppressionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MissingXmlCommentIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                public void {|CSENSE001:MyMethod|}() { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(3, 17, 3, 25).WithArguments("MyClass.MyMethod()").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task MissingParamTagIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                /// <summary>This is a summary.</summary>
                public void MyMethod(int {|CSENSE002:x|}) { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task DuplicateParamTagIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                /// <summary>This is a summary.</summary>
                /// <param name="x">First</param>
                /// {|CSENSE009:<param name="x">Second</param>|}
                public void MyMethod(int x) { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1571").WithSpan(5, 16, 5, 24).WithArguments("x").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task StrayParamTagIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                /// <summary>This is a summary.</summary>
                /// {|CSENSE003:<param name="y">Stray</param>|}
                public void MyMethod(int {|CSENSE002:x|}) { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1572").WithSpan(4, 22, 4, 23).WithArguments("y").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1573").WithSpan(5, 30, 5, 31).WithArguments("x", "MyClass.MyMethod(int)").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task InvalidCrefIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                /// <summary>See <see cref="{|CSENSE007:NonExistent|}"/>.</summary>
                public void MyMethod() { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1574").WithSpan(3, 33, 3, 44).WithArguments("NonExistent").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task MalformedCrefIsSuppressed()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
                /// <summary>See <see cref="{|CSENSE007:Invalid(|}"/>.</summary>
                public void MyMethod() { }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1584").WithSpan(3, 33, 3, 41).WithArguments("Invalid(").WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1658").WithSpan(3, 41, 3, 42).WithArguments("error CS1026: ) expected", "1026").WithIsSuppressed(true),
        };

        await VerifySuppressionAsync(testCode, expected);
    }

    [Test]
    public async Task ConstantIsUnsuppressedWithConditionalSuppression()
    {
        const string testCode = """
            public class MyClass
            {
                public const int X = 1;
            }
            """;

        // CS1591 is expected because:
        // 1. It is reported by the compiler (public const).
        // 2. CommentSense ignores it (ExcludeConstants = true).
        // 3. Suppressor checks eligibility, sees it's ignored, and decides NOT to suppress CS1591.
        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(1, 14, 1, 21).WithArguments("MyClass"),
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(3, 22, 3, 23).WithArguments("MyClass.X"),
        };

        var config = new Dictionary<string, string>
        {
            ["comment_sense.exclude_constants"] = "true",
            ["comment_sense.enable_conditional_suppression"] = "true"
        };

        await VerifySuppressionAsync(testCode, expected, config);
    }

    [Test]
    public async Task ProtectedMemberIsUnsuppressedWhenVisibilityIsPublic()
    {
        const string testCode = """
            public class MyClass
            {
                protected void M() { }
            }
            """;

        // CS1591 is expected because:
        // 1. It is reported by the compiler (protected member is visible).
        // 2. CommentSense ignores it (VisibilityLevel = Public).
        // 3. Suppressor checks eligibility, sees it's excluded, and decides NOT to suppress CS1591.

        // Revised Expected:
        // 1. CS1591 (MyClass) - Suppressed
        // 2. CSENSE001 (MyClass) - Active
        // 3. CS1591 (M) - Active (Unsuppressed)

        var expectedDiagnostics = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 21).WithArguments("MyClass").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(1, 14, 1, 21).WithArguments("MyClass"),
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(3, 20, 3, 21).WithArguments("MyClass.M()"),
        };

        var config = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "public",
            ["comment_sense.enable_conditional_suppression"] = "true"
        };

        await VerifySuppressionAsync(testCode, expectedDiagnostics, config);
    }

    [Test]
    public async Task DiagnosticWithoutSourceTreeIsSuppressed()
    {
        // 1. CS1591 at Location.None (from helper) should be suppressed by ShouldSuppress returning true early when tree is null.
        // 2. CS1591 for class C should be suppressed (default blanket suppression).
        // 3. CSENSE001 for class C should be reported.
        var expected = new[]
        {
            new DiagnosticResult("CS1591", DiagnosticSeverity.Warning).WithIsSuppressed(true),
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 15).WithArguments("C").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(1, 14, 1, 15).WithArguments("C"),
        };

        await VerifySuppressionAsync("public class C {}", expected, additionalAnalyzers: [new ReportDiagnosticAtNoneAnalyzer()]);
    }

    [Test]
    public async Task DiagnosticOnNonSymbolIsSuppressed()
    {
        // 1. CS1591 reported on a 'return' statement (not a declaration).
        // 2. GetDeclaredSymbol returns null.
        // 3. ShouldSuppress returns true (fallback to suppress).
        // 4. CS1591 for class C (default) suppressed.
        // 5. CSENSE001 for class C reported.
        const string testCode = """
            public class C
            {
                public int M()
                {
                    return 1;
                }
            }
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(1, 14, 1, 15).WithArguments("C").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(1, 14, 1, 15).WithArguments("C"),
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(3, 16, 3, 17).WithArguments("C.M()").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(3, 16, 3, 17).WithArguments("M"),
            new DiagnosticResult("CS1591", DiagnosticSeverity.Warning).WithSpan(5, 9, 5, 18).WithIsSuppressed(true), // "return" keyword span
        };

        var config = new Dictionary<string, string>
        {
            ["comment_sense.enable_conditional_suppression"] = "true"
        };

        await VerifySuppressionAsync(testCode, expected, config, additionalAnalyzers: [new ReportDiagnosticOnStatementAnalyzer()]);
    }

    [Test]
    public async Task DiagnosticOnUsingDirectiveIsSuppressed()
    {
        // 1. CS1591 reported on a 'using' directive.
        // 2. GetAssociatedSymbol returns null because 'using' is not a member and not in one.
        // 3. ShouldSuppress returns true (fallback to suppress).
        const string testCode = """
            using System;
            public class C {}
            """;

        var expected = new[]
        {
            DiagnosticResult.CompilerWarning("CS1591").WithSpan(2, 14, 2, 15).WithArguments("C").WithIsSuppressed(true),
            new DiagnosticResult(CommentSenseRules.MissingDocumentationRule).WithSpan(2, 14, 2, 15).WithArguments("C"),
            new DiagnosticResult("CS1591", DiagnosticSeverity.Warning).WithSpan(1, 1, 1, 14).WithIsSuppressed(true),
        };

        var config = new Dictionary<string, string>
        {
            ["comment_sense.enable_conditional_suppression"] = "true"
        };

        await VerifySuppressionAsync(testCode, expected, config, additionalAnalyzers: [new ReportDiagnosticOnUsingAnalyzer()]);
    }

#pragma warning disable RS1038, RS1041, RS1036
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class ReportDiagnosticOnUsingAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [new("CS1591", "Title", "Message", "Category", DiagnosticSeverity.Warning, true)];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(ctx =>
            {
                ctx.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], ctx.Node.GetLocation()));
            }, SyntaxKind.UsingDirective);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class ReportDiagnosticAtNoneAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [new("CS1591", "Title", "Message", "Category", DiagnosticSeverity.Warning, true)];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(ctx =>
                ctx.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], Location.None)), SymbolKind.NamedType);
        }
    }

    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    private sealed class ReportDiagnosticOnStatementAnalyzer : DiagnosticAnalyzer
    {
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            [new("CS1591", "Title", "Message", "Category", DiagnosticSeverity.Warning, true)];

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSymbolAction(ctx =>
            {
                if (ctx.Symbol is not IMethodSymbol { Name: "M" } method)
                    return;

                var syntaxRef = method.DeclaringSyntaxReferences.FirstOrDefault();

                var syntax = syntaxRef?.GetSyntax() as MethodDeclarationSyntax;
                if (syntax?.Body == null)
                    return;

                var returnStatement = syntax.Body.Statements.OfType<ReturnStatementSyntax>().FirstOrDefault();
                if (returnStatement != null)
                {
                    ctx.ReportDiagnostic(Diagnostic.Create(SupportedDiagnostics[0], returnStatement.GetLocation()));
                }
            }, SymbolKind.Method);
        }
    }
#pragma warning restore RS1038, RS1041, RS1036

    private static async Task VerifySuppressionAsync(string source, IEnumerable<DiagnosticResult>? expectedSuppressed = null, IDictionary<string, string>? configOptions = null, IEnumerable<DiagnosticAnalyzer>? additionalAnalyzers = null)
    {
        var analyzers = new List<DiagnosticAnalyzer> { new CommentSenseSuppressor() };
        if (additionalAnalyzers != null)
        {
            analyzers.AddRange(additionalAnalyzers);
        }

        await VerifyCSenseAsync(
            source,
            compilerDiagnostics: CompilerDiagnostics.Warnings,
            documentationMode: DocumentationMode.Diagnose,
            configOptions: configOptions,
            expectedDiagnostics: expectedSuppressed,
            additionalAnalyzers: analyzers,
            solutionTransform: (solution, projectId) =>
            {
                var project = solution.GetProject(projectId);
                if (project == null)
                    return solution;

                var compilationOptions = project.CompilationOptions;
                if (compilationOptions == null)
                    return solution;

                var specificOptions = compilationOptions.SpecificDiagnosticOptions
                                                        .SetItem("CS1591", ReportDiagnostic.Warn)
                                                        .SetItem("CS1573", ReportDiagnostic.Warn)
                                                        .SetItem("CS1572", ReportDiagnostic.Warn)
                                                        .SetItem("CS1571", ReportDiagnostic.Warn)
                                                        .SetItem("CS1584", ReportDiagnostic.Warn)
                                                        .SetItem("CS1574", ReportDiagnostic.Warn)
                                                        .SetItem("CS1658", ReportDiagnostic.Warn);

                return solution.WithProjectCompilationOptions(projectId, compilationOptions.WithSpecificDiagnosticOptions(specificOptions));
            });
    }
}
