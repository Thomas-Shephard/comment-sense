using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.SdkCompiler.Tests;

public class SuppressionTests
{
    [TestCase("union Choice(int, string)", "public", false, true)]
    [TestCase("union Choice(int, string)", "public", true, true)]
    [TestCase("union Choice(int, string)", "internal", true, false)]
    [TestCase("closed class State", "public", false, true)]
    [TestCase("closed class State", "public", true, true)]
    [TestCase("closed class State", "internal", true, false)]
    public async Task AppliesConditionalVisibility(string declaration, string accessibility, bool conditional, bool suppressed)
    {
        using var test = new CompilerTest($"/// <summary>See <see cref=\"Missing\"/>.</summary>\n{accessibility} {declaration};", options: new Dictionary<string, string>
        {
            ["comment_sense.enable_conditional_suppression"] = conditional.ToString()
        });
        var diagnostics = await test.AnalyzeAsync(suppress: true);
        var warnings = diagnostics.Where(d => d.Id == "CS1574").ToArray();
        Assert.That(warnings, Is.Not.Empty);
        Assert.That(warnings.All(d => d.IsSuppressed == suppressed), Is.True);
    }

    [Test]
    public async Task SuppressionCanBeDisabled()
    {
        using var test = new CompilerTest("public union Choice(int, string);",
            diagnosticOptions: new Dictionary<string, ReportDiagnostic> { ["CSENSESUP001"] = ReportDiagnostic.Suppress });
        var diagnostics = await test.AnalyzeAsync(suppress: true);
        Assert.That(diagnostics.Any(d => d.Id == "CS1591" && !d.IsSuppressed), Is.True);
    }
}
