using CommentSense.Analyzers;
using CommentSense.Core;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;
using NUnit.Framework;
using System.Reflection;

namespace CommentSense.CodeFixes.Tests;

public class LowQualityDocumentationCodeFixTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, LowQualityDocumentationCodeFixProvider>
{
    private static readonly Dictionary<string, string> QualityOptions = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "comment_sense.require_capitalization", "true" },
        { "comment_sense.require_ending_punctuation", "true" },
        { "comment_sense.similarity_threshold", "1.0" },
        { "comment_sense.min_summary_length", "0" }
    };

    [Test]
    public async Task FixesLowercaseStartAndMissingPunctuation()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>saves the item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves the item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task FixesMultiLineSummary()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>
                /// saves the item
                /// to the database
                /// </summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>
                /// Saves the item
                /// to the database.
                /// </summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task FixesCapitalizationAfterLeadingEmptyTag()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><see cref="C"/> saves the item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><see cref="C"/> Saves the item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodAfterTrailingEmptyTag()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Saves via <see cref="C"/></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves via <see cref="C"/>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task FixAllInDocument()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>saves the item</summary>|}
                /// {|CSENSE016:<param name="p">the item</param>|}
                public void M(int p) { }

                /// {|CSENSE016:<summary>another method</summary>|}
                public void M2() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves the item.</summary>
                /// <param name="p">The item.</param>
                public void M(int p) { }

                /// <summary>Another method.</summary>
                public void M2() { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task RespectsDisabledSettings()
    {
        var options = new Dictionary<string, string>
        {
            { "dotnet_diagnostic.CSENSE001.severity", "none" },
            { "comment_sense.require_capitalization", "false" },
            { "comment_sense.require_ending_punctuation", "true" },
            { "comment_sense.min_summary_length", "50" }
        };

        const string source = """
            /// <summary>This is a long enough summary for the class to be valid.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>saves the item to the database using a very long and descriptive summary</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>This is a long enough summary for the class to be valid.</summary>
            public class C
            {
                /// <summary>saves the item to the database using a very long and descriptive summary.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task FixesNestedTagContent()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><see cref="C">saves the item</see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><see cref="C">Saves the item</see>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task CapitalizesFirstLetterWhenItIsAnEntity()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>&#x61;bc</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Abc.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task DoesNotAddPeriodWhenDocumentationEndsWithPunctuationEntity()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text&#46;</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text&#46;</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodAfterTrailingNonPunctuationEntity()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Saves &amp;</summary>|}
                public void M1() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves &amp;.</summary>
                public void M1() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task DoesNotDuplicatePunctuationWhenReportedForSimilarity()
    {
        const string source2 = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>saves via <see cref="C">.</see></summary>|}
                public void M1() { }
            }
            """;
        const string fixedSource2 = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves via <see cref="C">.</see></summary>
                public void M1() { }
            }
            """;

        await VerifyCodeFixAsync(source2, fixedSource2, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodAfterTrailingEmptyElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>See <see cref="C"/></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>See <see cref="C"/>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodAfterMultipleTrailingEntities()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Saves &amp; &quot;</summary>|}
                public void M1() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Saves &amp; &quot;.</summary>
                public void M1() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task StartsWithNumberDoesNotCapitalizeAddsPeriod()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>1st item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>1st item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task StartsWithSymbolDoesNotCapitalizeAddsPeriod()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>_item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>_item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task StartsWithNonLetterElementCapitalizesNextText()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><c>123</c> text</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><c>123</c> Text.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task StartsWithElementWithLettersDoesNotCapitalizeNextText()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><c>ABC</c> text</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><c>ABC</c> text.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task TrailingWhitespaceInsertsPeriodBeforeWhitespace()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Text   </summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text.   </summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task TrailingElementWithPunctuationDoesNotAddPeriod()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see cref="C">ended.</see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <see cref="C">ended.</see></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task TrailingElementWithoutPunctuationAddsPeriodAfterElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Text <see cref="C">ended</see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <see cref="C">ended</see>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task TrailingEmptyElementAddsPeriodAfterElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>Text <see cref="C"/></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <see cref="C"/>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AlreadyCorrectNoChange()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>TEXT.</summary>|}
                public void M() { }
            }
            """;

        // Flag "TEXT" as low quality
        var options = new Dictionary<string, string>(QualityOptions)
        {
            ["comment_sense.low_quality_terms"] = "TEXT"
        };

        await VerifyNoCodeFixAsync(source, options);
    }

    [Test]
    public async Task RecursiveElementWithLettersDoesNotCapitalizeNextText()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><container><c>ABC</c></container> text</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><container><c>ABC</c></container> text.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task EmptyContentNoChange()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary></summary>|}
                public void M() { }
            }
            """;

        await VerifyNoCodeFixAsync(source, QualityOptions);
    }

    [Test]
    public async Task WhitespaceContentNoChange()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>   </summary>|}
                public void M() { }
            }
            """;

        await VerifyNoCodeFixAsync(source, QualityOptions);
    }

    [Test]
    public async Task SkipsCommentsInCapitalization()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><!-- comment --> text</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><!-- comment --> Text.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task SkipsCommentsWhenCheckingPunctuation()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text. <!-- comment --></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. <!-- comment --></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AddsPeriodBeforeTrailingComment()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <!-- comment --></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. <!-- comment --></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task CapitalizesFirstLetterInsideNestedElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><see cref="C">text</see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><see cref="C">Text</see>.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task SkipsEntitiesInPunctuationAddsPeriodAfterEntity()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text. &amp;</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. &amp;.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task DeeplyNestedPunctuationDoesNotAddPeriod()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <container><see cref="C">ended.</see></container></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <container><see cref="C">ended.</see></container></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodBeforeTrailingNestedEmptyElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see><see/></see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. <see><see/></see></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task NestedElementEndingWithWhitespaceAndCommentSkipsThem()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <container>ended. <!-- comment --> </container></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <container>ended. <!-- comment --> </container></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task NestedElementFollowedByCommentDoesNotAddDuplicatePeriod()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see>ended.</see><!-- comment --></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <see>ended.</see><!-- comment --></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task NestedCDataDoesNotPreventDetectingPunctuation()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see>ended. <![CDATA[ ]]> </see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text <see>ended. <![CDATA[ ]]> </see></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodBeforeTrailingEmptyElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see></see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. <see></see></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AppendsPeriodBeforeTrailingWhitespaceOnlyElement()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>text <see>   </see></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>Text. <see>   </see></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task CapitalizesFirstLetterInCData()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><![CDATA[saves the item]]></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><![CDATA[Saves the item.]]></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task AddsPeriodToCData()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><![CDATA[Saves the item]]></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><![CDATA[Saves the item.]]></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task HandlesEntityInHasAnyLetterCheck()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>&amp; saves the item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>&amp; saves the item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task HandlesNumericEntityInHasAnyLetterCheck()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary>&#x31; saves the item</summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary>&#x31; saves the item.</summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task HandlesNonLetterCDataAtBeginning()
    {
        const string source = """
            /// <summary>Class.</summary>
            public class C
            {
                /// {|CSENSE016:<summary><![CDATA[123 saves the item]]></summary>|}
                public void M() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Class.</summary>
            public class C
            {
                /// <summary><![CDATA[123 saves the item.]]></summary>
                public void M() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, QualityOptions);
    }

    [Test]
    public async Task FixDocumentAsyncReturnsDocumentWhenXmlElementCannotBeRelocated()
    {
        using var workspace = new AdhocWorkspace();
        var document = workspace.AddProject("Test", LanguageNames.CSharp).AddDocument("Test.cs", "public class C { }");
        var method = typeof(LowQualityDocumentationCodeFixProvider).GetMethod("FixDocumentAsync", BindingFlags.NonPublic | BindingFlags.Static)
                     ?? throw new InvalidOperationException();

        var invoked = method.Invoke(null, [document, new TextSpan(0, 1), CommentSenseOptions.Default, CancellationToken.None]);
        var task = invoked as Task<Document> ?? throw new InvalidOperationException();
        var result = await task;

        Assert.That(result, Is.EqualTo(document));
    }
}
