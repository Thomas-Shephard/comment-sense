using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Tests.Utilities;

public static class RoslynTestUtils
{
    private static readonly List<MetadataReference> CachedReferences = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select(a => MetadataReference.CreateFromFile(a.Location))
        .Cast<MetadataReference>()
        .ToList();

    public static ISymbol GetSymbolFromSource(string source, string symbolName, bool parseDocumentation = false)
    {
        var parseOptions = new CSharpParseOptions(documentationMode: parseDocumentation ? DocumentationMode.Parse : DocumentationMode.None);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, options: parseOptions);

        var compilation = CSharpCompilation.Create("TestAssembly",
                                               options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                                           .AddReferences(CachedReferences)
                                           .AddSyntaxTrees(syntaxTree);

        var diagnostics = compilation.GetDiagnostics();
        if (diagnostics.Any(d => d.Severity == DiagnosticSeverity.Error))
        {
            var errors = string.Join(Environment.NewLine, diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
            Assert.Fail($"Compilation failed:{Environment.NewLine}{errors}");
        }

        var semanticModel = compilation.GetSemanticModel(syntaxTree);

        // Find the symbol by name
        var root = syntaxTree.GetRoot();
        var declaration = root.DescendantNodes()
                              .First(n => n switch
                              {
                                  BaseTypeDeclarationSyntax b => b.Identifier.ValueText == symbolName,
                                  MethodDeclarationSyntax m => m.Identifier.ValueText == symbolName,
                                  ConstructorDeclarationSyntax c => c.Identifier.ValueText == symbolName,
                                  PropertyDeclarationSyntax p => p.Identifier.ValueText == symbolName,
                                  VariableDeclaratorSyntax v => v.Identifier.ValueText == symbolName,
                                  LabeledStatementSyntax l => l.Identifier.ValueText == symbolName,
                                  FromClauseSyntax f => f.Identifier.ValueText == symbolName,
                                  LetClauseSyntax l => l.Identifier.ValueText == symbolName,
                                  _ => false
                              });

        var symbol = semanticModel.GetDeclaredSymbol(declaration);

        return symbol ?? throw new InvalidOperationException($"Could not find symbol for '{symbolName}' in the provided source code.");
    }
}
