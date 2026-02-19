using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class OrderSynchronizationTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, OrderSynchronizationCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE016.severity", "none" },
        { "dotnet_diagnostic.CSENSE024.severity", "none" }
    };

    [Test]
    public void GetExpectedMemberNamesReturnsEmptyForUnsupportedTagName()
    {
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var result = SymbolExtensions.GetExpectedMemberNames(null!, "invalid");
        Assert.That(result.IsEmpty, Is.True);
    }

    [Test]
    public void GetExpectedMemberNamesReturnsEmptyForUnsupportedSymbol()
    {
        var syntaxTree = CSharpSyntaxTree.ParseText("class C { int F; }");
        var compilation = CSharpCompilation.Create("Test", [syntaxTree]);
        var root = syntaxTree.GetRoot();
        var fieldDecl = root.DescendantNodes().OfType<FieldDeclarationSyntax>().First();
        var semanticModel = compilation.GetSemanticModel(syntaxTree);
        var symbol = semanticModel.GetDeclaredSymbol(fieldDecl.Declaration.Variables.First()) ?? throw new InvalidOperationException();

        var resultParam = SymbolExtensions.GetExpectedMemberNames(symbol, "param");
        Assert.That(resultParam.IsEmpty, Is.True);

        var resultTypeParam = SymbolExtensions.GetExpectedMemberNames(symbol, "typeparam");
        Assert.That(resultTypeParam.IsEmpty, Is.True);
    }

    [Test]
    public void ReorderTagsReturnsDocumentIfInsufficientNamedTags()
    {
        var code = """
                   /// <summary>Docs</summary>
                   /// <param>Unnamed</param>
                   class C { }
                   """;
        var syntaxTree = CSharpSyntaxTree.ParseText(code);
        var root = syntaxTree.GetRoot();
        var docTrivia = root.DescendantNodes(descendIntoTrivia: true)
                            .OfType<DocumentationCommentTriviaSyntax>()
                            .First();

        CSharpCompilation.Create("Test", [syntaxTree]);
        using var workspace = new AdhocWorkspace();
        var doc = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", code);

        var result = OrderSynchronizationCodeFixProvider.ReorderTags(doc, root, docTrivia, "param", []);

        Assert.That(result, Is.EqualTo(doc));
    }

    [Test]
    public async Task FixOrderAsyncReturnsDocumentWhenNodeNotFound()
    {
        var code = "class C { }";
        using var workspace = new AdhocWorkspace();
        var doc = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", code);

        var tree = await doc.GetSyntaxTreeAsync();
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var diagnostic = Diagnostic.Create("ID", "Category", "Message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, location: Location.Create(tree!, new Microsoft.CodeAnalysis.Text.TextSpan(code.Length, 0)));

        var result = await OrderSynchronizationCodeFixProvider.FixOrderAsync(doc, diagnostic, CancellationToken.None);

        Assert.That(result, Is.EqualTo(doc));
    }

    [Test]
    public async Task FixOrderAsyncReturnsDocumentWhenNotXmlNode()
    {
        var code = "class C { }";
        using var workspace = new AdhocWorkspace();
        var doc = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", code);

        var tree = await doc.GetSyntaxTreeAsync();
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var diagnostic = Diagnostic.Create("ID", "Category", "Message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, location: Location.Create(tree!, new Microsoft.CodeAnalysis.Text.TextSpan(0, 5)));

        var result = await OrderSynchronizationCodeFixProvider.FixOrderAsync(doc, diagnostic, CancellationToken.None);

        Assert.That(result, Is.EqualTo(doc));
    }

    [Test]
    public async Task ReorderParameters()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// {|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                /// <param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderTypeParameters()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <typeparam name="T2">Second</typeparam>
                /// {|CSENSE010:<typeparam name="T1">First</typeparam>|}
                public void Method<T1, T2>() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <typeparam name="T1">First</typeparam>
                /// <typeparam name="T2">Second</typeparam>
                public void Method<T1, T2>() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderMultipleParametersFixAll()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// {|CSENSE008:<param name="p1">First</param>|}
                /// <param name="p4">Fourth</param>
                /// {|CSENSE008:<param name="p3">Third</param>|}
                public void Method(int p1, int p2, int p3, int p4) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                /// <param name="p2">Second</param>
                /// <param name="p3">Third</param>
                /// <param name="p4">Fourth</param>
                public void Method(int p1, int p2, int p3, int p4) { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderParametersPreservesInterleavedContent()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// <remarks>Some remarks</remarks>
                /// {|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                /// <remarks>Some remarks</remarks>
                /// <param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderParametersOnSameLine()
    {
        // Diagnostic location for p1
        const string sourceWithDiagnostic = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>{|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param><param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        await VerifyCodeFixAsync(sourceWithDiagnostic, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderRecordParameters()
    {
        const string source = """
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>Summary</summary>
            /// <param name="p2">Second</param>
            /// {|CSENSE008:<param name="p1">First</param>|}
            public record Test(int p1, int p2);
            """;
        const string fixedSource = """
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>Summary</summary>
            /// <param name="p1">First</param>
            /// <param name="p2">Second</param>
            public record Test(int p1, int p2);
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderDelegateParameters()
    {
        const string source = """
            /// <summary>Summary</summary>
            /// <param name="p2">Second</param>
            /// {|CSENSE008:<param name="p1">First</param>|}
            public delegate void TestDelegate(int p1, int p2);
            """;
        const string fixedSource = """
            /// <summary>Summary</summary>
            /// <param name="p1">First</param>
            /// <param name="p2">Second</param>
            public delegate void TestDelegate(int p1, int p2);
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderParametersWithDuplicatesPreservesBoth()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// {|CSENSE008:<param name="p1">First - Duplicate 1</param>|}
                /// <param name="p1">First - Duplicate 2</param>
                public void Method(int p1, int p2) { }
            }
            """;
        // Both p1 tags should come before p2, in their original relative order
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First - Duplicate 1</param>
                /// <param name="p1">First - Duplicate 2</param>
                /// <param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        // Disable CSENSE009 (duplicate param) to test reordering behavior specifically
        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "dotnet_diagnostic.CSENSE009.severity", "none" }
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task ReorderParametersWithStrayTags()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// <param name="stray">Stray</param>
                /// {|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        // Stray tags (not in expected order) should be pushed after sorted tags
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                /// <param name="p2">Second</param>
                /// <param name="stray">Stray</param>
                public void Method(int p1, int p2) { }
            }
            """;

        // Disable CSENSE003 (stray param) to test reordering behavior specifically
        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "dotnet_diagnostic.CSENSE003.severity", "none" }
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task ReorderIndexerParameters()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <value>Value</value>
                /// <param name="index2">Second</param>
                /// {|CSENSE008:<param name="index1">First</param>|}
                public int this[int index1, int index2] => 0;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <value>Value</value>
                /// <param name="index1">First</param>
                /// <param name="index2">Second</param>
                public int this[int index1, int index2] => 0;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderTypeParametersOnClass()
    {
        const string source = """
            /// <summary>Summary</summary>
            /// <typeparam name="T2">Second</typeparam>
            /// {|CSENSE010:<typeparam name="T1">First</typeparam>|}
            public class Test<T1, T2> { }
            """;
        const string fixedSource = """
            /// <summary>Summary</summary>
            /// <typeparam name="T1">First</typeparam>
            /// <typeparam name="T2">Second</typeparam>
            public class Test<T1, T2> { }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReorderTagsReturnsDocumentIfOnlyOneNamedTag()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                public void Method(int p1) { }
            }
            """;

        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var root = await syntaxTree.GetRootAsync();
        var docTrivia = root.DescendantNodes(descendIntoTrivia: true).OfType<DocumentationCommentTriviaSyntax>().First();

        using var workspace = new AdhocWorkspace();
        var doc = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", source);

        var result = OrderSynchronizationCodeFixProvider.ReorderTags(doc, root, docTrivia, "param", ["p1"]);
        Assert.That(result, Is.EqualTo(doc));
    }

    [Test]
    public async Task NestedTagsAreFlagged()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary {|CSENSE003:<param name="p1">Nested</param>|}</summary>
                /// <param name="p2">Second</param>
                /// {|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary <param name="p1">Nested</param></summary>
                /// <param name="p1">First</param>
                /// <param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        var expectedAfter = new DiagnosticResult("CSENSE003", DiagnosticSeverity.Warning).WithSpan(3, 26, 3, 57).WithArguments("p1");

        await VerifyCodeFixAsync(source, fixedSource, configOptions: DisableUnrelatedRules, expectedDiagnosticsAfter: [expectedAfter]);
    }

    [Test]
    public async Task TagWithoutNameIsIgnored()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p2">Second</param>
                /// <param>Unnamed</param>
                /// {|CSENSE008:<param name="p1">First</param>|}
                public void Method(int p1, int p2) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="p1">First</param>
                /// <param>Unnamed</param>
                /// <param name="p2">Second</param>
                public void Method(int p1, int p2) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task FixOrderAsyncReturnsDocumentWhenNoMemberDeclaration()
    {
        const string source = """
            /// <param name="p1">Docs</param>
            namespace N { }
            """;

        using var workspace = new AdhocWorkspace();
        var doc = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", source);
        var tree = await doc.GetSyntaxTreeAsync() ?? throw new InvalidOperationException();
        var root = await tree.GetRootAsync();
        var xmlNode = root.DescendantNodes(descendIntoTrivia: true).OfType<XmlNodeSyntax>().First();

        var diagnostic = Diagnostic.Create("ID", "Category", "Message", DiagnosticSeverity.Warning, DiagnosticSeverity.Warning, true, 1, location: xmlNode.GetLocation());

        var result = await OrderSynchronizationCodeFixProvider.FixOrderAsync(doc, diagnostic, CancellationToken.None);

        Assert.That(result, Is.EqualTo(doc));
    }
}
