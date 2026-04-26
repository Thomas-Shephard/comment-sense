using System.Xml.Linq;
using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class QualityAnalyzerTests
{
    [Test]
    public void IsLowQualityDifferentTextReturnsFalse()
    {
        var element = new XElement("summary", "Some text that is not the symbol name");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityEmptyElementReturnsTrue()
    {
        var element = new XElement("summary");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWhitespaceElementReturnsTrue()
    {
        var element = new XElement("summary", "   ");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameReturnsTrue()
    {
        var element = new XElement("summary", "MySymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySymbolNameCaseInsensitiveReturnsTrue()
    {
        var element = new XElement("summary", "mysymbol");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityWithNestedElementsReturnsFalse()
    {
        var element = new XElement("summary", new XElement("see", new XAttribute("cref", "T:System.Object")));
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityInternalNullContentReturnsTrue()
    {
        var result = QualityAnalyzer.IsLowQuality((string?)null, "Symbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityInternalWhitespaceContentReturnsTrue()
    {
        var result = QualityAnalyzer.IsLowQuality("   ", "Symbol", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityReturnsBranch()
    {
        var element = new XElement("returns", "return");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default, tagName: "returns");
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityReturnsKeywordBranch()
    {
        var element = new XElement("returns", "returns");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", CommentSenseOptions.Default, tagName: "returns");
        Assert.That(result, Is.True);
    }

    [Test]
    public void LevenshteinDistanceHeapAllocationBranch()
    {
        var longSymbolName = new string('A', 300);
        var longSummary = new string('A', 290) + "BBBBBBBBBB";

        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.9 };
        var element = new XElement("summary", longSummary);

        var result = QualityAnalyzer.IsLowQuality(element, longSymbolName, options);
        Assert.That(result, Is.True);
    }

    [Test]
    public void QualityAnalyzerNormalizationToEmptyReturnsTrue()
    {
        var options = CommentSenseOptions.Default with { RequireEndingPunctuation = true };
        var element = new XElement("summary", "...");
        var result = QualityAnalyzer.IsLowQuality(element, "MySymbol", options);
        Assert.That(result, Is.True);
    }

    [Test]
    public void CalculateSimilarityBelowThreshold()
    {
        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.9 };
        var result = QualityAnalyzer.IsLowQuality(new XElement("summary", "Different"), "Symbol", options);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityEmptySymbolNameReturnsFalse()
    {
        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.5 };
        var result = QualityAnalyzer.IsLowQuality("Content", string.Empty, options);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityLengthDifferenceEarlyExit()
    {
        var options = CommentSenseOptions.Default with { SimilarityThreshold = 0.8 };
        var result = QualityAnalyzer.IsLowQuality("Short", "VeryLongSymbolName", options);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityXElementNestedElementsWithSymbolReturnsFalse()
    {
        var symbol = GetRequiredNamedType(CreateCompilation("public class C { public int P { get; set; } }"), "C").GetMembers("P").Single();
        var element = new XElement("value", new XElement("see", new XAttribute("cref", "T:System.String")));

        var result = QualityAnalyzer.IsLowQuality(element, symbol, symbol, CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityXElementUsesTargetSymbolNameWhenDifferent()
    {
        var compilation = CreateCompilation("public class C { public int M() => 0; }");
        var type = GetRequiredNamedType(compilation, "C");
        var method = type.GetMembers("M").Single();
        var element = new XElement("returns", "C");

        var result = QualityAnalyzer.IsLowQuality(element, method, type, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityXElementUsesPrimarySymbolNameBeforeTypeChecks()
    {
        var compilation = CreateCompilation("public class C { public int M() => 0; }");
        var method = GetRequiredNamedType(compilation, "C").GetMembers("M").Single();
        var element = new XElement("returns", "M");

        var result = QualityAnalyzer.IsLowQuality(element, method, method, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityXElementUsesReturnTypeName()
    {
        var compilation = CreateCompilation("public class C { public int M() => 0; }");
        var method = GetRequiredNamedType(compilation, "C").GetMembers("M").Single();
        var element = new XElement("returns", "int");

        var result = QualityAnalyzer.IsLowQuality(element, method, method, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityXElementUsesSimpleTypeNameForGenericProperty()
    {
        var compilation = CreateCompilation("using System.Collections.Generic; public class C { public List<int> P { get; set; } = new(); }");
        var property = GetRequiredNamedType(compilation, "C").GetMembers("P").Single();
        var element = new XElement("value", "List");

        var result = QualityAnalyzer.IsLowQuality(element, property, property, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySyntaxNestedElementsWithSymbolReturnsFalse()
    {
        var symbol = GetRequiredNamedType(CreateCompilation("public class C { public int P { get; set; } }"), "C").GetMembers("P").Single();
        var element = ParseXmlElement("/// <value><see cref=\"string\"/></value>\nclass C {}");

        var result = QualityAnalyzer.IsLowQuality(element, symbol, symbol, CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualitySyntaxUsesTargetSymbolNameWhenDifferent()
    {
        var compilation = CreateCompilation("public class C { public int M() => 0; }");
        var type = GetRequiredNamedType(compilation, "C");
        var method = type.GetMembers("M").Single();
        var element = ParseXmlElement("/// <returns>C</returns>\nclass D {}");

        var result = QualityAnalyzer.IsLowQuality(element, method, type, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualitySyntaxWithTypeLessSymbolReturnsFalseForValidContent()
    {
        var type = (ISymbol)GetRequiredNamedType(CreateCompilation("public class C {}"), "C");
        var element = ParseXmlElement("/// <summary>Valid summary.</summary>\nclass D {}");

        var result = QualityAnalyzer.IsLowQuality(element, type, type, CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualitySyntaxUsesSimpleTypeNameForGenericProperty()
    {
        var compilation = CreateCompilation("using System.Collections.Generic; public class C { public List<int> P { get; set; } = new(); }");
        var property = GetRequiredNamedType(compilation, "C").GetMembers("P").Single();
        var element = ParseXmlElement("/// <value>List</value>\nclass D {}");

        var result = QualityAnalyzer.IsLowQuality(element, property, property, CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityForAnyFormatXElementNestedElementsReturnsFalse()
    {
        var element = new XElement("summary", new XElement("see", new XAttribute("cref", "T:System.String")));
        var result = QualityAnalyzer.IsLowQualityForAnyFormat(element, "Display", "Qualified.Display", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    [Test]
    public void IsLowQualityForAnyFormatXElementUsesContentPath()
    {
        var element = new XElement("summary", "Display");
        var result = QualityAnalyzer.IsLowQualityForAnyFormat(element, "Display", "Qualified.Display", CommentSenseOptions.Default);
        Assert.That(result, Is.True);
    }

    [Test]
    public void IsLowQualityForAnyFormatSyntaxNestedElementsReturnsFalse()
    {
        var element = ParseXmlElement("/// <summary><see cref=\"string\"/></summary>\nclass C {}");
        var result = QualityAnalyzer.IsLowQualityForAnyFormat(element, "Display", "Qualified.Display", CommentSenseOptions.Default);
        Assert.That(result, Is.False);
    }

    private static CSharpCompilation CreateCompilation(string source)
    {
        return CSharpCompilation.Create("Test")
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(List<>).Assembly.Location))
            .AddSyntaxTrees(CSharpSyntaxTree.ParseText(source));
    }

    private static XmlElementSyntax ParseXmlElement(string source)
    {
        return CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(documentationMode: DocumentationMode.Parse))
            .GetRoot()
            .DescendantNodes(descendIntoTrivia: true)
            .OfType<XmlElementSyntax>()
            .First();
    }

    private static INamedTypeSymbol GetRequiredNamedType(CSharpCompilation compilation, string metadataName)
    {
        return compilation.GetTypeByMetadataName(metadataName)
            ?? throw new InvalidOperationException($"Expected type '{metadataName}' was not found.");
    }
}
