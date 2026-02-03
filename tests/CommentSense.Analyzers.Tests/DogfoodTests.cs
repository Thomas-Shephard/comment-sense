using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class DogfoodTests
{
    [Test]
    public async Task Dogfood()
    {
        var repoRoot = ProjectLayout.RepositoryRoot;
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
                        new PackageIdentity("System.Collections.Immutable", "8.0.0")
                    ]),
            }
        };

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

        Assert.DoesNotThrowAsync(async () => await test.RunAsync());
    }

    private static IEnumerable<string> GetSourceFiles(string dir) =>
        Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories)
                 .Where(f =>
                     !f.Contains(Path.DirectorySeparatorChar + "obj" + Path.DirectorySeparatorChar) &&
                     !f.Contains(Path.DirectorySeparatorChar + "bin" + Path.DirectorySeparatorChar));
}
