using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public partial class DogfoodTests
{
    [Test]
    public Task DogfoodDefault()
    {
        return RunDogfoodAsync([]);
    }

    [Test]
    public Task DogfoodStrict()
    {
        return RunDogfoodAsync(new Dictionary<string, string>
        {
            { "comment_sense.min_summary_length", "10" },
            { "comment_sense.require_ending_punctuation", "true" },
            { "comment_sense.similarity_threshold", "0.7" },
            { "comment_sense.ghost_references.mode", "strict" },
            { "comment_sense.ignore_system_exceptions", "true" },
            { "comment_sense.langwords", "true, false, null, void, async, await" },
            { "comment_sense.allow_implicit_inheritdoc", "false" },
            { "comment_sense.exclude_constants", "false" },
            { "comment_sense.exclude_enums", "false" },
            { "comment_sense.low_quality_terms", "TODO, FIXME, N/A" }
        });
    }

    private static async Task RunDogfoodAsync(Dictionary<string, string> options)
    {
        var repoRoot = GetRepositoryRoot();
        var srcDir = Path.Combine(repoRoot, "src");

        var sourceFiles = GetSourceFiles(srcDir)
            .Select(path => (Name: Path.GetRelativePath(repoRoot, path), Path: path));

        var test = new CSharpAnalyzerTest<CommentSenseAnalyzer, NUnitVerifier>
        {
            TestState =
            {
                ReferenceAssemblies = ReferenceAssemblies.NetStandard.NetStandard20
                    .AddPackages([
                        new PackageIdentity("Microsoft.CodeAnalysis.CSharp", "5.0.0"),
                        new PackageIdentity("Microsoft.CodeAnalysis.CSharp.Workspaces", "5.0.0"),
                        new PackageIdentity("System.Collections.Immutable", "8.0.0")
                    ]),
            }
        };

        if (options.Count > 0)
        {
            var configText = "is_global = true\n" + string.Join("\n", options.Select(kv => $"{kv.Key} = {kv.Value}"));
            test.TestState.AnalyzerConfigFiles.Add(("/.editorconfig", configText));
        }

        test.TestState.Sources.Add(("GlobalUsings.cs", """
            global using System;
            global using System.Collections.Generic;
            global using System.IO;
            global using System.Linq;
            global using System.Threading;
            global using System.Threading.Tasks;
            """));

        var sources = await Task.WhenAll(sourceFiles.Select(async sourceFile =>
            (sourceFile.Name, Content: await File.ReadAllTextAsync(sourceFile.Path))));

        foreach (var source in sources)
        {
            test.TestState.Sources.Add(source);
        }

        AddMockResources(test.TestState, srcDir);

        Assert.DoesNotThrowAsync(async () => await test.RunAsync());
    }

    private static void AddMockResources(SolutionState testState, string srcDir)
    {
        foreach (var resxFile in Directory.GetFiles(srcDir, "Resources.resx", SearchOption.AllDirectories))
        {
            var projectDir = Path.GetDirectoryName(resxFile);
            var namespaceName = Path.GetFileName(projectDir);  // Matches the namespace: CommentSense.Analyzers, CommentSense.CodeFixes

            var resourceNames = ResourceNameRegex().Matches(File.ReadAllText(resxFile))
                                      .Select(m => m.Groups[1].Value)
                                      .ToList();

            var mockSource = $$"""
                namespace {{namespaceName}}
                {
                    internal class Resources
                    {
                        public static global::System.Resources.ResourceManager ResourceManager => null;
                        {{string.Join("\n        ", resourceNames.Select(name => $"public static string {name} => null;"))}}
                    }
                }
                """;

            testState.Sources.Add(($"{namespaceName}.Resources.g.cs", mockSource));
        }
    }

    private static IEnumerable<string> GetSourceFiles(string dir) =>
        Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                 .Where(f =>
                     !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                     !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));

    private static string GetRepositoryRoot()
    {
        var dir = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(dir))
        {
            if (File.Exists(Path.Combine(dir, "CommentSense.slnx")))
            {
                return dir;
            }
            dir = Path.GetDirectoryName(dir);
        }

        throw new DirectoryNotFoundException("Could not find repository root.");
    }

    [System.Text.RegularExpressions.GeneratedRegex("<data name=\"([^\"]+)\"")]
    private static partial System.Text.RegularExpressions.Regex ResourceNameRegex();
}
