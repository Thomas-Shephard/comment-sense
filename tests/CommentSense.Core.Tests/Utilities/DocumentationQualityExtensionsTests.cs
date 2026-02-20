using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Core.Tests.Utilities;

public class DocumentationQualityExtensionsTests
{
    private static readonly XmlEmptyElementSyntax SeeNode = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("see"));
    private static readonly XmlTextSyntax TextWithPuncNode = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test.")));
    private static readonly XmlTextSyntax TextWithoutPuncNode = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")));
    private static readonly XmlTextSyntax WhitespaceNode = SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(" ")));
    private static readonly XmlCDataSectionSyntax CDataWithPuncNode = SyntaxFactory.XmlCDataSection(
        SyntaxFactory.Token(SyntaxKind.XmlCDataStartToken),
        SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test.")),
        SyntaxFactory.Token(SyntaxKind.XmlCDataEndToken));

    [TestCaseSource(nameof(GetTextTokensCases))]
    public void GetTextTokensReturnsExpectedResult(XmlNodeSyntax node, bool expectEmpty)
    {
        var tokens = node.GetTextTokens();
        Assert.That(tokens, expectEmpty ? Is.Empty : Is.Not.Empty);
    }

    private static IEnumerable<TestCaseData> GetTextTokensCases()
    {
        yield return new TestCaseData(TextWithPuncNode, false).SetName("GetTextTokens_XmlText_ReturnsTokens");
        yield return new TestCaseData(CDataWithPuncNode, false).SetName("GetTextTokens_CData_ReturnsTokens");
        yield return new TestCaseData(SeeNode, true).SetName("GetTextTokens_EmptyElement_ReturnsEmpty");
    }

    [TestCaseSource(nameof(WithTextTokensCases))]
    public void WithTextTokensReturnsExpectedNode(XmlNodeSyntax node, string newText, bool expectUpdate)
    {
        var newTokens = SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral(newText));
        var result = node.WithTextTokens(newTokens);

        if (expectUpdate)
        {
            var tokens = result.GetTextTokens();
            Assert.That(tokens[0].ValueText, Is.EqualTo(newText));
        }
        else
        {
            Assert.That(result, Is.SameAs(node));
        }
    }

    private static IEnumerable<TestCaseData> WithTextTokensCases()
    {
        yield return new TestCaseData(TextWithPuncNode, "new text", true).SetName("WithTextTokens_XmlText_UpdatesNode");
        yield return new TestCaseData(CDataWithPuncNode, "new cdata", true).SetName("WithTextTokens_CData_UpdatesNode");
        yield return new TestCaseData(SeeNode, "ignored", false).SetName("WithTextTokens_NonTextNode_ReturnsOriginal");
    }

    [TestCase("abc", true)]
    [TestCase("  abc", true)]
    [TestCase("Abc", false)]
    [TestCase("1abc", false)]
    [TestCase("", false)]
    [TestCase("   ", false)]
    public void StartsWithLowercaseReturnsExpectedValue(string content, bool expected)
    {
        Assert.That(DocumentationQualityExtensions.StartsWithLowercase(content), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(StringPunctuationCases))]
    public void GetPunctuationStateForStringReturnsExpectedValue(string content, bool trimEnd, object expected)
    {
        Assert.That(DocumentationQualityExtensions.GetPunctuationState(content, trimEnd), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> StringPunctuationCases()
    {
        yield return new TestCaseData("test.", true, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_String_EndsWithPeriod_ReturnsYes");
        yield return new TestCaseData("test!", true, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_String_EndsWithExclamation_ReturnsYes");
        yield return new TestCaseData("test?", true, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_String_EndsWithQuestion_ReturnsYes");
        yield return new TestCaseData("test", true, DocumentationQualityExtensions.PunctuationState.No).SetName("GetPunctuationState_String_NoPunctuation_ReturnsNo");
        yield return new TestCaseData("test ", false, DocumentationQualityExtensions.PunctuationState.No).SetName("GetPunctuationState_String_TrailingWhitespace_NoTrim_ReturnsNo");
        yield return new TestCaseData("", true, DocumentationQualityExtensions.PunctuationState.Meaningless).SetName("GetPunctuationState_String_Empty_ReturnsMeaningless");
        yield return new TestCaseData("   ", true, DocumentationQualityExtensions.PunctuationState.Meaningless).SetName("GetPunctuationState_String_WhitespaceOnly_ReturnsMeaningless");
    }

    [TestCase("test.", true)]
    [TestCase("test", false)]
    [TestCase("test. ", false, false)]
    public void EndsWithPunctuationForStringReturnsExpectedValue(string content, bool expected, bool trimEnd = true)
    {
        Assert.That(DocumentationQualityExtensions.EndsWithPunctuation(content, trimEnd), Is.EqualTo(expected));
    }

    [TestCaseSource(nameof(NodePunctuationCases))]
    public void GetPunctuationStateForXmlNodeReturnsExpectedValue(XmlNodeSyntax node, object expected)
    {
        Assert.That(node.GetPunctuationState(), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> NodePunctuationCases()
    {
        yield return new TestCaseData(TextWithPuncNode, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_XmlText_WithPunc_ReturnsYes");
        yield return new TestCaseData(TextWithoutPuncNode, DocumentationQualityExtensions.PunctuationState.No).SetName("GetPunctuationState_XmlText_NoPunc_ReturnsNo");
        yield return new TestCaseData(WhitespaceNode, DocumentationQualityExtensions.PunctuationState.Meaningless).SetName("GetPunctuationState_XmlText_Whitespace_ReturnsMeaningless");
        yield return new TestCaseData(CDataWithPuncNode, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_CData_WithPunc_ReturnsYes");
        yield return new TestCaseData(SeeNode, DocumentationQualityExtensions.PunctuationState.Meaningless).SetName("GetPunctuationState_EmptyElement_NoAttrs_ReturnsMeaningless");

        var seeWithCref = SyntaxFactory.XmlEmptyElement(
            SyntaxFactory.XmlName("see"),
            SyntaxFactory.List<XmlAttributeSyntax>([
                SyntaxFactory.XmlCrefAttribute(SyntaxFactory.TypeCref(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword))))
            ]));
        yield return new TestCaseData(seeWithCref, DocumentationQualityExtensions.PunctuationState.No).SetName("GetPunctuationState_EmptyElement_WithAttrs_ReturnsNo");

        var summary = SyntaxFactory.XmlElement(SyntaxFactory.XmlName("summary"), SyntaxFactory.SingletonList<XmlNodeSyntax>(TextWithPuncNode));
        yield return new TestCaseData(summary, DocumentationQualityExtensions.PunctuationState.Yes).SetName("GetPunctuationState_XmlElement_ReturnsContentState");

        var comment = SyntaxFactory.XmlComment(
            SyntaxFactory.Token(SyntaxKind.XmlCommentStartToken),
            SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("test")),
            SyntaxFactory.Token(SyntaxKind.XmlCommentEndToken));
        yield return new TestCaseData(comment, DocumentationQualityExtensions.PunctuationState.Meaningless).SetName("GetPunctuationState_XmlComment_ReturnsMeaningless");
    }

    [TestCaseSource(nameof(ListPunctuationCases))]
    public void GetPunctuationStateForSyntaxListReturnsExpectedValue(SyntaxList<XmlNodeSyntax> list, object expectedState, bool expectedEndsWith)
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.That(list.GetPunctuationState(), Is.EqualTo(expectedState));
            Assert.That(list.EndsWithPunctuation(), Is.EqualTo(expectedEndsWith));
        }
    }

    private static IEnumerable<TestCaseData> ListPunctuationCases()
    {
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([TextWithPuncNode, WhitespaceNode]), DocumentationQualityExtensions.PunctuationState.Yes, true).SetName("GetPunctuationState_List_EndsWithPuncThenWhitespace_ReturnsYes");
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([TextWithoutPuncNode, WhitespaceNode]), DocumentationQualityExtensions.PunctuationState.No, false).SetName("GetPunctuationState_List_EndsWithNoPuncThenWhitespace_ReturnsNo");
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([WhitespaceNode]), DocumentationQualityExtensions.PunctuationState.Meaningless, false).SetName("GetPunctuationState_List_WhitespaceOnly_ReturnsMeaningless");
    }

    [TestCaseSource(nameof(HasAnyLetterCases))]
    public void HasAnyLetterReturnsExpectedValue(SyntaxList<XmlNodeSyntax> list, bool expected)
    {
        Assert.That(list.HasAnyLetter(), Is.EqualTo(expected));
    }

    private static IEnumerable<TestCaseData> HasAnyLetterCases()
    {
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([TextWithPuncNode]), true).SetName("HasAnyLetter_TextWithLetter_ReturnsTrue");
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("123"))), CDataWithPuncNode]), true).SetName("HasAnyLetter_CDataWithLetter_ReturnsTrue");
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([SyntaxFactory.XmlText(SyntaxFactory.TokenList(SyntaxFactory.XmlTextLiteral("123")))]), false).SetName("HasAnyLetter_NoLetters_ReturnsFalse");

        var elementWithLetter = SyntaxFactory.XmlElement(SyntaxFactory.XmlName("c"), SyntaxFactory.SingletonList<XmlNodeSyntax>(TextWithPuncNode));
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([elementWithLetter]), true).SetName("HasAnyLetter_NestedElementWithLetter_ReturnsTrue");

        var elementWithoutLetter = SyntaxFactory.XmlElement(SyntaxFactory.XmlName("c"), SyntaxFactory.SingletonList<XmlNodeSyntax>(WhitespaceNode));
        yield return new TestCaseData(SyntaxFactory.List<XmlNodeSyntax>([elementWithoutLetter]), false).SetName("HasAnyLetter_NestedElementWithoutLetter_ReturnsFalse");
    }
}
