using NUnit.Framework;

namespace CommentSense.Core.Tests;

public class DocumentationTagsTests
{
    [Test]
    public void TagOrderContainsExpectedTags()
    {
        var order = DocumentationTags.TagOrder;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(order, Contains.Key(DocumentationTags.InheritDoc));
            Assert.That(order, Contains.Key(DocumentationTags.Summary));
            Assert.That(order, Contains.Key(DocumentationTags.Param));
            Assert.That(order, Contains.Key(DocumentationTags.TypeParam));
            Assert.That(order, Contains.Key(DocumentationTags.Returns));
            Assert.That(order, Contains.Key(DocumentationTags.Value));
            Assert.That(order, Contains.Key(DocumentationTags.Exception));
            Assert.That(order, Contains.Key(DocumentationTags.Remarks));
        }
    }

    [Test]
    public void TagOrderProvidesCorrectRelativeOrdering()
    {
        var order = DocumentationTags.TagOrder;
        using (Assert.EnterMultipleScope())
        {
            Assert.That(order[DocumentationTags.InheritDoc], Is.LessThan(order[DocumentationTags.Summary]));
            Assert.That(order[DocumentationTags.Summary], Is.LessThan(order[DocumentationTags.Param]));
            Assert.That(order[DocumentationTags.Param], Is.LessThan(order[DocumentationTags.Returns]));
            Assert.That(order[DocumentationTags.Returns], Is.EqualTo(order[DocumentationTags.Value]));
            Assert.That(order[DocumentationTags.Returns], Is.LessThan(order[DocumentationTags.Exception]));
            Assert.That(order[DocumentationTags.Exception], Is.LessThan(order[DocumentationTags.Remarks]));
        }
    }
}
