using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Tests.Utilities;

public static class RoslynTestUtils
{
    private static readonly List<MetadataReference> CachedReferences = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select<System.Reflection.Assembly, MetadataReference>(a => MetadataReference.CreateFromFile(a.Location))
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
                              .FirstOrDefault(n => n switch
                              {
                                  BaseTypeDeclarationSyntax baseType => baseType.Identifier.ValueText == symbolName,
                                  MethodDeclarationSyntax method => method.Identifier.ValueText == symbolName,
                                  ConstructorDeclarationSyntax constructor => constructor.Identifier.ValueText == symbolName,
                                  PropertyDeclarationSyntax property => property.Identifier.ValueText == symbolName,
                                  VariableDeclaratorSyntax variable => variable.Identifier.ValueText == symbolName,
                                  LabeledStatementSyntax labeledStatement => labeledStatement.Identifier.ValueText == symbolName,
                                  FromClauseSyntax fromClause => fromClause.Identifier.ValueText == symbolName,
                                  LetClauseSyntax letClause => letClause.Identifier.ValueText == symbolName,
                                  _ => false
                              });

        if (declaration == null)
        {
            throw new InvalidOperationException($"Could not find declaration for '{symbolName}' in the provided source code.");
        }

        var symbol = semanticModel.GetDeclaredSymbol(declaration);

        return symbol ?? throw new InvalidOperationException($"Could not find symbol for '{symbolName}' in the provided source code.");
    }
}
