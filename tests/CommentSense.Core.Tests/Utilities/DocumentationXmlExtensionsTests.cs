using System.Xml.Linq;
using CommentSense.Core.Utilities;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationXmlExtensionsTests
{
    [Test]
    public void HasValidDocumentationWithSummaryReturnsTrue()
    {
        const string xml = "<summary>Test</summary>";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationWithInheritDocReturnsTrue()
    {
        const string xml = "<inheritdoc />";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.True);
    }

    [Test]
    public void HasValidDocumentationMalformedXmlReturnsFalse()
    {
        const string xml = "<invalid";
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation(xml), Is.False);
    }

    [Test]
    public void HasValidDocumentationNullInputReturnsFalse()
    {
        Assert.That(DocumentationXmlExtensions.HasValidDocumentation((string?)null), Is.False);
    }

    [Test]
    public void TryParseDocumentationValidXmlReturnsTrueAndElement()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation("<summary>Test</summary>", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.True);
            Assert.That(element.Descendants("summary").Any(), Is.True);
        }
    }

    [Test]
    public void TryParseDocumentationInvalidXmlReturnsFalseAndEmptyElement()
    {
        var result = DocumentationXmlExtensions.TryParseDocumentation("<invalid", out var element);
        using (Assert.EnterMultipleScope())
        {
            Assert.That(result, Is.False);
            Assert.That(element, Is.Not.Null);
        }
    }

    [Test]
    public void GetParamNamesExistingParamsReturnsNames()
    {
        const string xml = "<member><param name=\"p1\">p1</param></member>";
        var result = DocumentationXmlExtensions.GetParamNames(xml).ToList();
        Assert.That(result, Contains.Item("p1"));
    }

    [Test]
    public void GetTypeParamNamesExistingTypeParamsReturnsNames()
    {
        const string xml = "<member><typeparam name=\"T\">T</typeparam></member>";
        var result = DocumentationXmlExtensions.GetTypeParamNames(xml).ToList();
        Assert.That(result, Contains.Item("T"));
    }

    [Test]
    public void HasReturnsTagExistingTagReturnsTrue()
    {
        const string xml = "<returns>Test</returns>";
        Assert.That(DocumentationXmlExtensions.HasReturnsTag(xml), Is.True);
    }

    [Test]
    public void HasValueTagOnXElementReturnsTrue()
    {
        var element = XElement.Parse("<root><value>Test</value></root>");
        Assert.That(DocumentationXmlExtensions.HasValueTag(element), Is.True);
    }

    [Test]
    public void GetExceptionCrefsExistingExceptionsReturnsCrefs()
    {
        const string xml = "<exception cref=\"Ex\">Test</exception>";
        var result = DocumentationXmlExtensions.GetExceptionCrefs(xml).ToList();
        Assert.That(result, Contains.Item("Ex"));
    }

    [Test]
    public void GetTargetElementsRecursiveSearchReturnsDeepNodes()
    {
        var root = XElement.Parse("<root><summary><see cref='T'/></summary></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root, "see", recursive: true).ToList();
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetTargetElementsNullTagNameRecursiveReturnsAllNodes()
    {
        var root = XElement.Parse("<root><summary><see/></summary></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root, tagName: null, recursive: true).ToList();
        Assert.That(result, Has.Count.EqualTo(2));
    }

    [Test]
    public void GetTargetElementsNullTagNameNotRecursiveReturnsTopLevelNodes()
    {
        var root = XElement.Parse("<root><summary><see/></summary></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root, tagName: null, recursive: false).ToList();
        Assert.That(result, Has.Count.EqualTo(1));
    }

    [Test]
    public void GetTargetElementsWithMemberRootReturnsChildren()
    {
        var root = XElement.Parse("<member><summary/></member>");
        var result = DocumentationXmlExtensions.GetTargetElements(root).ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name.LocalName, Is.EqualTo("summary"));
    }

    [Test]
    public void GetTargetElementsWithMemberChildReturnsGrandchildren()
    {
        var root = XElement.Parse("<root><member><summary/></member></root>");
        var result = DocumentationXmlExtensions.GetTargetElements(root).ToList();
        Assert.That(result, Has.Count.EqualTo(1));
        Assert.That(result[0].Name.LocalName, Is.EqualTo("summary"));
    }

    [Test]
    public void GetElementAttributeValuesTopLevelOnlyReturnsOnlyDirectChildren()
    {
        var root = XElement.Parse("<root><param name='p1'/><summary><param name='p2'/></summary></root>");
        var result = DocumentationXmlExtensions.GetElementAttributeValues(root, "param", "name", topLevelOnly: true).ToList();
        Assert.That(result, Contains.Item("p1"));
        Assert.That(result, Does.Not.Contain("p2"));
    }

    [Test]
    public void IsTopLevelWithDirectChildReturnsTrue()
    {
        var root = XElement.Parse("<root><summary/></root>");
        var element = root.Element("summary") ?? throw new InvalidOperationException();
        Assert.That(DocumentationXmlExtensions.IsTopLevel(root, element), Is.True);
    }

    [Test]
    public void IsTopLevelWithNestedChildReturnsFalse()
    {
        var root = XElement.Parse("<root><remarks><summary/></remarks></root>");
        var element = root.Element("remarks")?.Element("summary") ?? throw new InvalidOperationException();
        Assert.That(DocumentationXmlExtensions.IsTopLevel(root, element), Is.False);
    }

    [Test]
    public void IsTopLevelWithDetachedElementReturnsFalse()
    {
        var root = XElement.Parse("<root><summary/></root>");
        var element = new XElement("summary");
        Assert.That(DocumentationXmlExtensions.IsTopLevel(root, element), Is.False);
    }
}
