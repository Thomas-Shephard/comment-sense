using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Moq;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationLocationExtensionsTests
{
    [Test]
    public void GetDocumentationCommentTriviaWithTriviaReturnsTrivia()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var tree = CSharpSyntaxTree.ParseText(source);
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Not.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaWithRegularCommentReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("// regular comment\npublic class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaWithDirectiveReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("#if true\npublic class C {}\n#endif");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnDetachedNodeReturnsNull()
    {
        var detached = SyntaxFactory.IdentifierName("x");
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(detached), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnCompilationUnitReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("using System;");
        var node = tree.GetRoot();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationCommentTriviaOnMemberWithoutTriviaReturnsNull()
    {
        var tree = CSharpSyntaxTree.ParseText("public class C {}");
        var node = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>().First();
        Assert.That(DocumentationLocationExtensions.GetDocumentationCommentTrivia(node), Is.Null);
    }

    [Test]
    public void GetDocumentationLocationsExistingTagReturnsLocations()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var locations = symbol.GetDocumentationLocations("summary");
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetDocumentationLocationsMissingTagReturnsEmpty()
    {
        const string source = "public class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var locations = symbol.GetDocumentationLocations("summary");
        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void GetDocumentationLocationsOnMetadataSymbolReturnsEmpty()
    {
        var tree = CSharpSyntaxTree.ParseText("class C {}");
        var compilation = CSharpCompilation.Create("Test", [tree], [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);
        var obj = compilation.GetSpecialType(SpecialType.System_Object);
        var locations = obj.GetDocumentationLocations("summary");
        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void GetDocumentationLocationsOnFieldDeclaratorReturnsLocations()
    {
        const string source = "public class C { \n/// <summary>S</summary>\npublic int f1, f2; }";
        var symbol = GetSymbolFromSource(source, "f2");
        var locations = symbol.GetDocumentationLocations("summary");
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetDocumentationLocationsWithNullAttributeValueMatchesTagNameOnly()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var locations = symbol.GetDocumentationLocations("summary", attributeValue: null);
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetTargetElementsWithLocationsExistingTagIteratesLocations()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var symbol = GetSymbolFromSource(source, "C");

        var result = symbol.GetTargetElementsWithLocations("summary").ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0], Is.Not.EqualTo(Location.None));
    }

    [Test]
    public void GetTargetElementsWithLocationsMissingTagReturnsEmpty()
    {
        const string source = "public class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var result = symbol.GetTargetElementsWithLocations("summary").ToList();
        Assert.That(result, Is.Empty);
    }

    [Test]
    public void GetTargetElementsWithLocationsXElementOverloadIteratesElementsAndLocations()
    {
        const string source = "/// <summary>S</summary>\npublic class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var xml = System.Xml.Linq.XElement.Parse("<member><summary>S</summary></member>");

        var result = symbol.GetTargetElementsWithLocations(xml, "summary").ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result[0].Element.Name.LocalName, Is.EqualTo("summary"));
            Assert.That(result[0].Location, Is.Not.EqualTo(Location.None));
        }
    }

    [Test]
    public void GetDocumentationLocationsTopLevelOnlyFalseReturnsDeepLocations()
    {
        const string source = "/// <summary><see cref=\"T\"/></summary>\npublic class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var locations = symbol.GetDocumentationLocations("see", topLevelOnly: false);
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void GetDocumentationLocationAllAttributeTypesReturnsCorrectLocations()
    {
        const string source = "public class C {\n/// <exception cref=\"System.Exception\">Ex</exception>\n/// <param name=\"x\">X</param>\n/// <mytag myattr=\"val\">Val</mytag>\npublic void M(int x) {}\n}";
        var symbol = GetSymbolFromSource(source, "M");

        var loc1 = symbol.GetDocumentationLocation("exception", "System.Exception", attributeName: "cref");
        Assert.That(loc1, Is.Not.EqualTo(Location.None));

        var loc2 = symbol.GetDocumentationLocation("param", "x", attributeName: "name");
        Assert.That(loc2, Is.Not.EqualTo(Location.None));

        var loc3 = symbol.GetDocumentationLocation("mytag", "val", attributeName: "myattr");
        Assert.That(loc3, Is.Not.EqualTo(Location.None));

        // Test Cref with T: prefix
        var loc4 = symbol.GetDocumentationLocation("exception", "T:System.Exception", attributeName: "cref");
        Assert.That(loc4, Is.Not.EqualTo(Location.None));
    }

    [Test]
    public void GetDocumentationLocationMultipleAttributesMatchesCorrectValue()
    {
        const string source = "public class C {\n/// <mytag a1=\"v1\" a2=\"v2\">Val</mytag>\npublic void M() {}\n}";
        var symbol = GetSymbolFromSource(source, "M");
        var loc = symbol.GetDocumentationLocation("mytag", "v2", attributeName: "a2");
        Assert.That(loc, Is.Not.EqualTo(Location.None));
    }

    [Test]
    public void GetDocumentationLocationsEmptyElementMatchesCorrectValue()
    {
        const string source = "public class C {\n/// <inheritdoc cref=\"System.Object\" />\npublic override string ToString() => \"\";\n}";
        var symbol = GetSymbolFromSource(source, "ToString");
        var locations = symbol.GetDocumentationLocations("inheritdoc", "System.Object", attributeName: "cref");
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void MatchAttributeMismatchedValueReturnsFalse()
    {
        var attr = SyntaxFactory.XmlTextAttribute(
            SyntaxFactory.XmlName("name"),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken),
            SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("wrong")),
            SyntaxFactory.Token(SyntaxKind.DoubleQuoteToken));
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "name", "right"), Is.False);
    }

    [Test]
    public void MatchAttributeCrefWithPrefixMismatchReturnsFalse()
    {
        var cref = SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        var attr = SyntaxFactory.XmlCrefAttribute(cref);
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "cref", "T:string"), Is.False);
    }

    [Test]
    public void MatchAttributeCrefMismatchedNameReturnsFalse()
    {
        var cref = SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        var attr = SyntaxFactory.XmlCrefAttribute(cref);
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "wrong", "int"), Is.False);
    }

    [Test]
    public void MatchAttributeCrefMismatchedValueReturnsFalse()
    {
        var cref = SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        var attr = SyntaxFactory.XmlCrefAttribute(cref);
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "cref", "wrong"), Is.False);
    }

    [Test]
    public void MatchAttributeCrefWithTPrefixReturnsTrue()
    {
        var cref = SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)));
        var attr = SyntaxFactory.XmlCrefAttribute(cref);
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "cref", "T:int"), Is.True);
    }

    [Test]
    public void MatchAttributeInheritDocWithoutValueReturnsTrue()
    {
        const string source = "public class C {\n/// <inheritdoc />\npublic void M() {}\n}";
        var symbol = GetSymbolFromSource(source, "M");
        var locations = symbol.GetDocumentationLocations("inheritdoc", attributeValue: null);
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void MatchAttributeInheritDocMismatchedValueReturnsFalse()
    {
        const string source = "public class C {\n/// <inheritdoc cref=\"T1\" />\npublic void M() {}\n}";
        var symbol = GetSymbolFromSource(source, "M");
        var locations = symbol.GetDocumentationLocations("inheritdoc", "T2", attributeName: "cref");
        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void MatchAttributeElementWithValueReturnsTrue()
    {
        const string source = "public class C {\n/// <summary name=\"val\">S</summary>\npublic void M() {}\n}";
        var symbol = GetSymbolFromSource(source, "M");
        var locations = symbol.GetDocumentationLocations("summary", "val", attributeName: "name");
        Assert.That(locations, Has.Length.EqualTo(1));
    }

    [Test]
    public void MatchAttributeElementWithMismatchedValueReturnsFalse()
    {
        const string source = "public class C {\n/// <summary name=\"val\">S</summary>\npublic void M() {}\n}";
        var symbol = GetSymbolFromSource(source, "M");
        var locations = symbol.GetDocumentationLocations("summary", "wrong", attributeName: "name");
        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void MatchAttributeNameAttributeMismatchedReturnsFalse()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <param name=\"val\" />\nclass C {}");
        var attr = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlNameAttributeSyntax>().First();
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "right", "val"), Is.False);
    }

    [Test]
    public void MatchAttributeTextAttributeMismatchedReturnsFalse()
    {
        var tree = CSharpSyntaxTree.ParseText("/// <summary wrong=\"val\" />\nclass C {}");
        var attr = tree.GetRoot().DescendantNodes(descendIntoTrivia: true).OfType<XmlTextAttributeSyntax>().First();
        Assert.That(DocumentationLocationExtensions.MatchAttribute(attr, "right", "val"), Is.False);
    }

    [Test]
    public void GetDocumentationLocationsNoTriviaReturnsEmpty()
    {
        const string source = "public class C {}";
        var symbol = GetSymbolFromSource(source, "C");
        var locations = symbol.GetDocumentationLocations("summary");
        Assert.That(locations, Is.Empty);
    }

    [Test]
    public void GetPrimaryLocationSingleLocationReturnsLocation()
    {
        var location = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var locations = ImmutableArray.Create(location);
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(location));
    }

    [Test]
    public void GetPrimaryLocationEmptyListReturnsNone()
    {
        var locations = ImmutableArray<Location>.Empty;
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(Location.None));
    }

    [Test]
    public void GetPrimaryLocationDefaultArrayReturnsNone()
    {
        var locations = default(ImmutableArray<Location>);
        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(Location.None));
    }

    [Test]
    public void GetLocationOrDefaultDefaultArrayReturnsSymbolLocation()
    {
        var loc = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([loc]);

        var locations = default(ImmutableArray<Location>);
        Assert.That(locations.GetLocationOrDefault(0, mockSymbol.Object), Is.EqualTo(loc));
    }

    [Test]
    public void GetLocationOrDefaultEmptyArrayReturnsSymbolLocation()
    {
        var loc = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([loc]);

        var locations = ImmutableArray<Location>.Empty;
        Assert.That(locations.GetLocationOrDefault(0, mockSymbol.Object), Is.EqualTo(loc));
    }

    [Test]
    public void GetLocationOrDefaultNegativeIndexReturnsSymbolLocation()
    {
        var loc = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([loc]);

        var locations = ImmutableArray.Create(Location.None);
        Assert.That(locations.GetLocationOrDefault(-1, mockSymbol.Object), Is.EqualTo(loc));
    }

    [Test]
    public void GetLocationOrDefaultLargeIndexReturnsSymbolLocation()
    {
        var loc = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([loc]);

        var locations = ImmutableArray.Create(Location.None);
        Assert.That(locations.GetLocationOrDefault(1, mockSymbol.Object), Is.EqualTo(loc));
    }

    [Test]
    public void GetLocationOrDefaultValidIndexReturnsLocation()
    {
        var loc = Location.Create("test.cs", new Microsoft.CodeAnalysis.Text.TextSpan(0, 0), new Microsoft.CodeAnalysis.Text.LinePositionSpan());
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([]);

        var locations = ImmutableArray.Create(loc);
        Assert.That(locations.GetLocationOrDefault(0, mockSymbol.Object), Is.EqualTo(loc));
    }

    [Test]
    public void GetLocationOrDefaultSymbolWithoutLocationsReturnsNone()
    {
        var mockSymbol = new Mock<ISymbol>();
        mockSymbol.Setup(s => s.Locations).Returns([]);

        var locations = ImmutableArray<Location>.Empty;
        Assert.That(locations.GetLocationOrDefault(0, mockSymbol.Object), Is.EqualTo(Location.None));
    }

    private static ISymbol GetSymbolFromSource(string source, string symbolName)
    {
        return RoslynTestUtils.GetSymbolFromSource(source, symbolName, parseDocumentation: true);
    }
}
