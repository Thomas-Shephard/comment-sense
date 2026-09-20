using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InitializerExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task ImplicitConstructorInitializers(
        [Values("class", "record")] string kind,
        [Values("public", "private")] string visibility,
        [Values] bool isStatic,
        [Values] bool property,
        [Values] bool documented)
    {
        var missing = !isStatic && !documented;
        var source = $$"""
            /// <summary>Stores a value.</summary>
            {{(documented ? "/// <exception cref=\"System.ArgumentException\">The value is invalid.</exception>" : "")}}
            public {{kind}} {{(missing ? "{|CSENSE012:Container|}" : "Container")}}
            {
                /// <summary>Stores the initialized value.</summary>
                {{(property ? "/// <value>The stored value.</value>" : "")}}
                {{visibility}} {{(isStatic ? "static" : "")}} int Stored {{(property ? "{ get; }" : "")}} =
                    System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: missing, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ImplicitConstructorCallsUseTypeDocumentation([Values] bool scan)
    {
        var source = $$"""
            /// <summary>Stores a value.</summary>
            /// <exception cref="System.ArgumentException">The value is invalid.</exception>
            public class Container
            {
                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }

            /// <summary>Creates storage.</summary>
            public class Factory
            {
                /// <summary>Creates a container.</summary>
                /// <returns>The storage.</returns>
                public Container {{(scan ? "{|CSENSE012:Create|}" : "Create")}}() => new Container();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: scan, referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
    }

    [TestCase(true)]
    [TestCase(false)]
    public void ConstructorsInDifferentFilesRespectTheirScanningOptions(bool firstScan)
    {
        var first = $$"""
            /// <summary>Stores a value.</summary>
            public partial class Container
            {
                /// <summary>Creates the storage.</summary>
                public {{(firstScan ? "{|CSENSE012:Container|}" : "Container")}}() { }

                private int Stored { get; } = Create();

                /// <summary>Creates a value.</summary>
                /// <returns>The value.</returns>
                /// <exception cref="System.ArgumentException">The value is invalid.</exception>
                private static int Create() => 1;
            }
            """;
        var second = $$"""
            public partial class Container
            {
                /// <summary>Creates the storage with an option.</summary>
                /// <param name="option">The construction option.</param>
                public {{(!firstScan ? "{|CSENSE012:Container|}" : "Container")}}(int option) { }
            }
            """;
        var test = new CSharpAnalyzerTest<CommentSenseAnalyzer, NUnitVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
                Sources = { ("/First.cs", first), ("/Second.cs", second) },
                AnalyzerConfigFiles =
                {
                    ("/.editorconfig", $$"""
                        root = true
                        [First.cs]
                        comment_sense.scan_called_methods_for_exceptions = {{firstScan}}
                        [Second.cs]
                        comment_sense.scan_called_methods_for_exceptions = {{!firstScan}}
                        """)
                }
            },
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        Assert.DoesNotThrowAsync(async () => await test.RunAsync());
    }

    [Test]
    public async Task PrimaryConstructorInitializers(
        [Values("class", "struct")] string kind,
        [Values("public", "private")] string visibility,
        [Values] bool isStatic,
        [Values] bool property,
        [Values] bool documented)
    {
        var missing = !isStatic && !documented;
        var source = $$"""
            /// <summary>Stores a value.</summary>
            /// <param name="value">The initial value.</param>
            {{(documented ? "/// <exception cref=\"System.ArgumentException\">The value is invalid.</exception>" : "")}}
            public {{kind}} {{(missing ? "{|CSENSE012:Container|}" : "Container")}}(int value)
            {
                /// <summary>Stores the initialized value.</summary>
                {{(property ? "/// <value>The stored value.</value>" : "")}}
                {{visibility}} {{(isStatic ? "static" : "")}} int Stored {{(property ? "{ get; }" : "")}} =
                    {{(isStatic ? "System.DateTime.Now.Ticks" : "value")}} > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: missing, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ExplicitConstructorInitializers(
        [Values("class", "struct")] string kind,
        [Values("public", "private")] string visibility,
        [Values] bool isStatic,
        [Values] bool property,
        [Values] bool documented)
    {
        var missing = !isStatic && !documented;
        var source = $$"""
            /// <summary>Stores a value.</summary>
            public {{kind}} Container
            {
                /// <summary>Creates the storage.</summary>
                {{(documented ? "/// <exception cref=\"System.ArgumentException\">The value is invalid.</exception>" : "")}}
                public {{(missing ? "{|CSENSE012:Container|}" : "Container")}}() { }

                /// <summary>Stores the initialized value.</summary>
                {{(property ? "/// <value>The stored value.</value>" : "")}}
                {{visibility}} {{(isStatic ? "static" : "")}} int Stored {{(property ? "{ get; }" : "")}} =
                    System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: missing, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [TestCase(true, null)]
    [TestCase(false, "System.ArgumentException")]
    public void ConstructorSuggestionsOnlyIncludeInstanceInitializersForInstanceConstructors(bool isStatic, string? expected)
    {
        var source = $$"""
            class C
            {
                int value = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
                {{(isStatic ? "static" : "public")}} C() {}
            }
            """;
        var compilation = CSharpCompilation.Create("Test", [CSharpSyntaxTree.ParseText(source)],
            [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var type = compilation.GetTypeByMetadataName("C") ?? throw new InvalidOperationException();
        var constructor = (isStatic ? type.StaticConstructors : type.InstanceConstructors).Single();

        Assert.That(ExceptionAnalyzer.FindBestMatchingThrownException(constructor, "ArgumentException",
            CommentSenseOptions.Default, compilation, CancellationToken.None), Is.EqualTo(expected));
    }

    [TestCase(true)]
    [TestCase(false)]
    public void InitializerInAnotherPartialDeclaration(bool primary)
    {
        var source = $$"""
            /// <summary>Stores a value.</summary>
            public partial class {{(primary ? "{|CSENSE012:Container|}()" : "Container")}}
            {
                {{(primary ? "" : "/// <summary>Creates the storage.</summary>\npublic {|CSENSE012:Container|}() { }")}}
            }
            """;
        const string otherPart = """
            public partial class Container
            {
                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        var test = new CSharpAnalyzerTest<CommentSenseAnalyzer, NUnitVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.Net.Net100,
                Sources = { source, otherPart }
            },
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };
        test.ApplyCommonConfiguration(null, DocumentationMode.Parse, null);
        Assert.DoesNotThrowAsync(async () => await test.RunAsync());
    }

    [Test]
    public async Task DelegatingConstructorUsesTargetDocumentation(
        [Values("class", "struct")] string kind, [Values] bool scan)
    {
        var source = $$"""
            /// <summary>Stores a value.</summary>
            public {{kind}} Container
            {
                /// <summary>Creates the storage.</summary>
                public {{(scan ? "{|CSENSE012:Container|}" : "Container")}}() : this(1) { }

                /// <summary>Creates the storage with an option.</summary>
                /// <param name="option">The construction option.</param>
                /// <exception cref="System.ArgumentException">The value is invalid.</exception>
                public Container(int option) { }

                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: scan, referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
    }

    [Test]
    public async Task StructConstructorCallingImplicitDefaultRunsInitializers()
    {
        const string source = """
            /// <summary>Stores a value.</summary>
            public struct Container
            {
                /// <summary>Creates the storage.</summary>
                /// <param name="option">The construction option.</param>
                public {|CSENSE012:Container|}(int option) : this() { }

                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task UnresolvedConstructorInitializerDoesNotAssumeInstanceInitialization()
    {
        const string source = """
            /// <summary>Stores a value.</summary>
            public struct Container
            {
                /// <summary>Creates the storage.</summary>
                /// <param name="option">The construction option.</param>
                public Container(int option) : this("unfinished") { }

                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: false, compilerDiagnostics: CompilerDiagnostics.None,
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task RecordCopyConstructorDoesNotRunInitializers()
    {
        const string source = """
            /// <summary>Stores a value.</summary>
            public record Container
            {
                /// <summary>Creates the storage.</summary>
                public {|CSENSE012:Container|}() { }

                /// <summary>Copies the storage.</summary>
                /// <param name="other">The storage to copy.</param>
                protected Container(Container other) { }

                private int Stored { get; } = System.DateTime.Now.Ticks > 0 ? 1 : throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task InitializerCallsRespectScanningOption(
        [Values] bool primary, [Values] bool scan)
    {
        var name = scan ? "{|CSENSE012:Container|}" : "Container";
        var source = $$"""
            /// <summary>Stores a value.</summary>
            public class {{(primary ? name + "()" : "Container")}}
            {
                {{(primary ? "" : "/// <summary>Creates the storage.</summary>\npublic " + name + "() { }")}}

                private int Stored { get; } = Create();

                /// <summary>Creates a value.</summary>
                /// <returns>The initialized value.</returns>
                /// <exception cref="System.ArgumentException">The value is invalid.</exception>
                private static int Create() => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: scan, referenceAssemblies: ReferenceAssemblies.Net.Net100,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = scan.ToString() });
    }

    [Test]
    public async Task InitializerDoesNotExecuteLambda([Values] bool primary)
    {
        var source = $$"""
            /// <summary>Stores a deferred operation.</summary>
            public class Container{{(primary ? "()" : "")}}
            {
                {{(primary ? "" : "/// <summary>Creates the storage.</summary>\npublic Container() { }")}}

                /// <summary>Gets the deferred operation.</summary>
                /// <value>The stored operation.</value>
                public System.Func<int> Operation { get; } = () => throw new System.ArgumentException();
            }
            """;

        await VerifyCSenseAsync(source, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }
}
