using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class KeywordToSeeLangwordTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, KeywordToSeeLangwordCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE006.severity", "none" }
    };

    [Test]
    public async Task ReplaceKeywordWithLangwordInSummary()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Returns {|CSENSE019:null|} if not found.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Returns <see langword="null" /> if not found.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReplaceMultipleKeywordsInBatch()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Returns {|CSENSE019:null|} or {|CSENSE019:void|} but {|CSENSE019:true|} is also possible.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Returns <see langword="null" /> or <see langword="void" /> but <see langword="true" /> is also possible.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReplaceKeywordAtStartOfLine()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// {|CSENSE019:null|} is the result.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// <see langword="null" /> is the result.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReplaceKeywordAtEndOfLine()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Result is {|CSENSE019:null|}
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Result is <see langword="null" />
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task NormalizeKeywordCasing()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Returns {|CSENSE019:Null|} or {|CSENSE019:TRUE|}.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Returns <see langword="null" /> or <see langword="true" />.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RespectCustomCasingFromConfiguration()
    {
        var customOptions = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "comment_sense.langwords", "Null, True" }
        };

        const string source = """
            public class Test
            {
                /// <summary>
                /// Returns {|CSENSE019:null|} or {|CSENSE019:true|}.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Returns <see langword="Null" /> or <see langword="True" />.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, customOptions);
    }

    [Test]
    public async Task DoNotReplaceInsideCodeTag()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// This is <code>null</code> here.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, source, DisableUnrelatedRules);
    }

    [Test]
    public async Task ReplaceKeywordDirectlyInDocumentationComment()
    {
        const string source = """
            /// {|CSENSE019:null|}
            public class Test
            {
            }
            """;
        const string fixedSource = """
            /// <see langword="null" />
            public class Test
            {
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAtAbsoluteStartOfXmlText()
    {
        const string source = """
            public class Test
            {
                /// <summary><see cref="Test"/>{|CSENSE019:null|} is the result.</summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary><see cref="Test"/><see langword="null" /> is the result.</summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAtAbsoluteEndOfXmlText()
    {
        const string source = """
            public class Test
            {
                /// <summary>Result is {|CSENSE019:null|}</summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Result is <see langword="null" /></summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAfterEntity()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// &amp;{|CSENSE019:null|}
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// &amp;<see langword="null" />
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordBeforeEntity()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// {|CSENSE019:null|}&amp;
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// <see langword="null" />&amp;
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAtStartOfLineWithNoSpace()
    {
        const string source = """
            public class Test
            {
                ///{|CSENSE019:null|}
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                ///<see langword="null" />
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAtStartOfLineAfterEntityWithNoSpace()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// &amp;
                ///{|CSENSE019:null|}
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// &amp;
                ///<see langword="null" />
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordFollowedByAnotherTagAtEndOfLine()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// {|CSENSE019:null|}<see cref="Test"/>
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// <see langword="null" /><see cref="Test"/>
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task KeywordAtEndOfBlockComment()
    {
        const string source = """
            public class Test
            {
                /** {|CSENSE019:null|}*/
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /** <see langword="null" />*/
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task CodeActionTitleUsesCanonicalCasing()
    {
        var customOptions = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "comment_sense.langwords", "Null" }
        };

        const string source = """
            public class Test
            {
                /// <summary>
                /// Returns {|CSENSE019:null|}.
                /// </summary>
                public object GetItem() => null;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Returns <see langword="Null" />.
                /// </summary>
                public object GetItem() => null;
            }
            """;

        await VerifyCodeFixTitleAsync(source, fixedSource, "Use <see langword=\"Null\" />", customOptions);
    }

    [Test]
    public void GetCanonicalKeywordReturnsMatchWhenFound()
    {
        string[] langwords = ["null", "true", "void"];
        var result = KeywordToSeeLangwordCodeFixProvider.GetCanonicalKeyword(langwords, "NULL");
        Assert.That(result, Is.EqualTo("null"));
    }

    [Test]
    public void GetCanonicalKeywordReturnsKeywordWhenNotFound()
    {
        string[] langwords = ["null"];
        var result = KeywordToSeeLangwordCodeFixProvider.GetCanonicalKeyword(langwords, "void");
        Assert.That(result, Is.EqualTo("void"));
    }
}
