using System.IO.Compression;
using System.Xml.Linq;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Vsix.Tests;

public class VsixPackageTests
{
    private string _vsixPath = string.Empty;

    [SetUp]
    public void Setup()
    {
        var solutionRoot = ProjectLayout.RepositoryRoot;
        var foundPath = Path.Combine(solutionRoot, "artifacts", "vsix", "CommentSense.vsix");

        if (!File.Exists(foundPath))
            Assert.Fail("Could not find any .vsix file. Build the 'CommentSense.Vsix' project with the 'CreateVsixContainer' target.");

        _vsixPath = foundPath;
    }

    [Test]
    public void VsixShouldContainManifestWithCorrectMetadata()
    {
        using var archive = ZipFile.OpenRead(_vsixPath);
        var manifestEntry = archive.GetEntry("extension.vsixmanifest");
        Assert.That(manifestEntry, Is.Not.Null, "extension.vsixmanifest not found in VSIX.");

        using var stream = manifestEntry.Open();
        var doc = XDocument.Load(stream);

        XNamespace ns = "http://schemas.microsoft.com/developer/vsx-schema/2011";
        var identity = doc.Descendants(ns + "Identity").FirstOrDefault();

        Assert.That(identity, Is.Not.Null, "Identity element not found in manifest.");

        var publisher = identity.Attribute("Publisher")?.Value;
        var version = identity.Attribute("Version")?.Value;

        using (Assert.EnterMultipleScope())
        {
            Assert.That(publisher, Is.Not.Null.And.Not.Empty, "Publisher should be set in manifest.");
            Assert.That(version, Is.Not.Null.And.Not.Empty, "Version should be set in manifest.");
        }

        using (Assert.EnterMultipleScope())
        {
            Assert.That(publisher, Is.Not.EqualTo("PUBLISHER_PLACEHOLDER"), "Publisher should have been updated from the placeholder by the build process.");

            Assert.That(version, Is.Not.EqualTo("0.0.0"), "Version should have been updated from the 0.0.0 placeholder by the build process.");
            // VSIX Identity version must be strictly numeric (major.minor.build.revision)
            Assert.That(version, Does.Match(@"^\d+\.\d+\.\d+\.\d+$"), $"Version '{version}' is not a valid 4-part numeric VSIX version.");
        }
    }

    [Test]
    public void VsixShouldContainAnalyzerAssemblies()
    {
        using var archive = ZipFile.OpenRead(_vsixPath);

        var hasAnalyzerDll = archive.Entries.Any(e => e.FullName.EndsWith("CommentSense.Analyzers.dll", StringComparison.OrdinalIgnoreCase));
        var hasCoreDll = archive.Entries.Any(e => e.FullName.EndsWith("CommentSense.Core.dll", StringComparison.OrdinalIgnoreCase));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(hasAnalyzerDll, Is.True, "CommentSense.Analyzers.dll not found in VSIX.");
            Assert.That(hasCoreDll, Is.True, "CommentSense.Core.dll not found in VSIX.");
        }
    }
}
