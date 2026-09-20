using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InheritDocScopeTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [TestCase(null, false, false, false, false)]
    [TestCase("/summary", false, false, false, false)]
    [TestCase("/returns", true, false, false, false)]
    [TestCase("/param", false, true, true, false)]
    [TestCase("/param[@name='first']", false, true, false, false)]
    [TestCase("/param[2]", false, false, true, false)]
    [TestCase("/typeparam", false, false, false, true)]
    [TestCase("/summary | /returns | /param[@name='first']", true, true, false, false)]
    [TestCase("/*", true, true, true, true)]
    [TestCase("/", true, true, true, true)]
    [TestCase("", true, true, true, true)]
    [TestCase("/returns/node()", false, false, false, false)]
    [TestCase("[", false, false, false, false)]
    [TestCase("count(/param)", false, false, false, false)]
    public async Task MethodInheritanceOnlyCoversSelectedTags(string? path, bool returns, bool first, bool second, bool typeParameter)
    {
        var documentation = path is null
            ? "<summary><inheritdoc cref=\"Source{T}\"/></summary>"
            : $"<inheritdoc cref=\"Source{{T}}\" path=\"{path}\"/>";
        var source = $$"""
            /// <summary>Documented methods.</summary>
            public class Example
            {
                /// <summary>Reads input.</summary>
                /// <typeparam name="T">The input type.</typeparam>
                /// <param name="first">The first input.</param>
                /// <param name="second">The second input.</param>
                /// <returns>The input value.</returns>
                public int Source<T>(int first, int second) => first;

                /// {{documentation}}
                public int {{(returns ? "Target" : "{|CSENSE006:Target|}")}}<{{(typeParameter ? "T" : "{|CSENSE004:T|}")}}>(int {{(first ? "first" : "{|CSENSE002:first|}")}}, int {{(second ? "second" : "{|CSENSE002:second|}")}}) => first;
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !(returns && first && second && typeParameter));
    }

    [TestCase(null, false)]
    [TestCase("/summary", false)]
    [TestCase("/value", true)]
    [TestCase("/*", true)]
    [TestCase("/value/node()", false)]
    public async Task PropertyInheritanceOnlyCoversSelectedTags(string? path, bool value)
    {
        var documentation = path is null
            ? "<summary><inheritdoc cref=\"Source\"/></summary>"
            : $"<inheritdoc cref=\"Source\" path=\"{path}\"/>";
        var source = $$"""
            /// <summary>Documented properties.</summary>
            public class Example
            {
                /// <summary>Gets the input.</summary>
                /// <value>The input value.</value>
                public int Source => 1;

                /// {{documentation}}
                public int {{(value ? "Target" : "{|CSENSE014:Target|}")}} => 1;
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !value);
    }

    [TestCase(null, false, false)]
    [TestCase("/summary", false, false)]
    [TestCase("/exception", true, true)]
    [TestCase("/*", true, true)]
    [TestCase("/exception[@cref='T:System.ArgumentException']", true, false)]
    [TestCase("/exception[2]", false, true)]
    [TestCase("/exception/node()", false, false)]
    public async Task ExceptionInheritanceOnlyCoversSelectedTypes(string? path, bool argument, bool operation)
    {
        var documentation = path is null
            ? "<summary><inheritdoc cref=\"Source\"/></summary>"
            : $"<inheritdoc cref=\"Source\" path=\"{path}\"/>";
        var target = "Target";
        if (!argument)
            target = "{|CSENSE012:" + target + "|}";
        if (!operation)
            target = "{|CSENSE012:" + target + "|}";
        var source = $$"""
            /// <summary>Documented methods.</summary>
            public class Example
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="System.ArgumentException">Invalid input.</exception>
                /// <exception cref="System.InvalidOperationException">Invalid state.</exception>
                public void Source() { }

                /// {{documentation}}
                public void {{target}}()
                {
                    if (System.DateTime.Now.Ticks > 0)
                        throw new System.ArgumentException();
                    throw new System.InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !argument || !operation);
    }

    [Test]
    public async Task InheritanceChainPreservesSummaryOnlyScope()
    {
        const string source = """
            /// <summary>Documented methods.</summary>
            public class Example
            {
                /// <summary>Reads input.</summary>
                /// <param name="value">The input.</param>
                /// <returns>The result.</returns>
                /// <exception cref="System.ArgumentException">Invalid input.</exception>
                public int Source(int value) => value;

                /// <inheritdoc cref="Source" path="/summary"/>
                public void Middle() { }

                /// <inheritdoc cref="Middle"/>
                public int {|CSENSE006:{|CSENSE012:Target|}|}(int {|CSENSE002:value|}) => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source);
    }

    [Test]
    public async Task SeparateInheritDocElementsKeepTheirOwnPaths()
    {
        const string source = """
            /// <summary>Documented methods.</summary>
            public class Example
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="System.ArgumentException">Invalid input.</exception>
                public void First() { }

                /// <summary>Performs other work.</summary>
                /// <exception cref="System.InvalidOperationException">Invalid state.</exception>
                public void Second() { }

                /// <inheritdoc cref="First" path="/summary"/>
                /// <inheritdoc cref="Second" path="/exception"/>
                public void {|CSENSE012:Target|}() => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source);
    }

    [TestCase("/summary", false)]
    [TestCase("/*", true)]
    public async Task ImplicitInheritanceRespectsPath(string path, bool complete)
    {
        var target = complete ? "Read" : "{|CSENSE006:{|CSENSE012:Read|}|}";
        var source = $$"""
            /// <summary>Reads input.</summary>
            public interface IReader
            {
                /// <summary>Reads a value.</summary>
                /// <param name="value">The input.</param>
                /// <returns>The result.</returns>
                /// <exception cref="System.ArgumentException">Invalid input.</exception>
                int Read(int value);
            }
            /// <summary>Reads input.</summary>
            public class Reader : IReader
            {
                /// <inheritdoc path="{{path}}"/>
                public int {{target}}(int {{(complete ? "value" : "{|CSENSE002:value|}")}}) => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !complete);
    }

    [TestCase("/summary", false)]
    [TestCase("/exception", true)]
    public async Task ReferencedDocumentationRespectsPath(string path, bool documented)
    {
        var source = $$"""
            /// <summary>Performs work.</summary>
            public class Worker
            {
                /// <inheritdoc cref="Contract.Execute" path="{{path}}"/>
                public void {{(documented ? "Execute" : "{|CSENSE012:Execute|}")}}() => throw new System.ArgumentException();
            }
            """;
        const string referenced = """
            public class Contract
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="System.ArgumentException">Invalid input.</exception>
                public void Execute() { }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !documented,
            solutionTransform: (solution, projectId) => AddReference(solution, projectId, referenced));
    }

    [TestCase("/summary", false)]
    [TestCase("", true)]
    public async Task SummaryPathDoesNotInheritUnknownMetadataDocumentation(string path, bool documented)
    {
        var source = $$"""
            /// <summary>Performs work.</summary>
            public class Worker
            {
                /// <inheritdoc cref="Contract.Execute" path="{{path}}"/>
                public void {{(documented ? "Execute" : "{|CSENSE012:Execute|}")}}() => throw new System.ArgumentException();
            }
            """;
        const string referenced = """
            public class Contract
            {
                /// <inheritdoc cref="M:Missing.Execute"/>
                public void Execute() { }
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !documented,
            solutionTransform: (solution, projectId) => AddReference(solution, projectId, referenced));
    }

    [TestCase("/summary", false)]
    [TestCase("/include", true)]
    [TestCase("/*", true)]
    public async Task IncludeOnlyExemptsValidationWhenSelected(string path, bool documented)
    {
        var source = $$"""
            /// <summary>Performs work.</summary>
            public class Worker
            {
                /// <summary>Performs work.</summary>
                /// <include file="Docs.xml" path="/doc/member/*"/>
                public void Source() { }

                /// <inheritdoc cref="Source" path="{{path}}"/>
                public int {{(documented ? "Execute" : "{|CSENSE006:{|CSENSE012:Execute|}|}")}}(int {{(documented ? "value" : "{|CSENSE002:value|}")}}) => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: !documented);
    }

    [Test]
    public async Task FilteringEmptyInheritedDocumentationReportsMissingTags()
    {
        const string source = """
            /// <summary>Performs work.</summary>
            public class Worker
            {
                /// <summary>Performs work.</summary>
                public void Source() { }

                /// <inheritdoc cref="Source" path="/remarks"/>
                public void Middle() { }

                /// <inheritdoc cref="Middle" path="/returns"/>
                public int {|CSENSE006:Target|}() => 1;
            }
            """;

        await VerifyCSenseAsync(source);
    }

    private static Solution AddReference(Solution solution, ProjectId projectId, string source)
    {
        var project = solution.GetProject(projectId) ?? throw new InvalidOperationException();
        var compilation = CSharpCompilation.Create("Contract",
            [CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(documentationMode: DocumentationMode.Diagnose))],
            project.MetadataReferences, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        using var assembly = new MemoryStream();
        using var documentation = new MemoryStream();
        Assert.That(compilation.Emit(assembly, xmlDocumentationStream: documentation).Success, Is.True);
        return solution.AddMetadataReference(projectId, MetadataReference.CreateFromImage(assembly.ToArray(),
            documentation: XmlDocumentationProvider.CreateFromBytes(documentation.ToArray())));
    }
}
