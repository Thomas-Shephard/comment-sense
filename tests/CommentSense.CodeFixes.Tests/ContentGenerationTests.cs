using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class ContentGenerationTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, ContentGenerationCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
    { "dotnet_diagnostic.CSENSE016.severity", "none" },
    { "dotnet_diagnostic.CSENSE001.severity", "none" }
    };

    [Test]
    public async Task AddMissingSummaryWhenNoDocumentation()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            public void {|CSENSE001:Method|}() { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>TODO</summary>
            public void Method() { }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE001.severity"] = "warning"
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingParamWhenDocumentationExists()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public void Method(int {|CSENSE002:x|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param name="x">TODO</param>
            public void Method(int x) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingInheritDoc()
    {
        const string source = """
        /// <summary>Base class</summary>
        public class Base
        {
            /// <summary>Base method</summary>
            public virtual void Method() { }
        }
        /// <summary>Derived class</summary>
        public class Derived : Base
        {
            public override void {|CSENSE018:Method|}() { }
        }
        """;
        const string fixedSource = """
        /// <summary>Base class</summary>
        public class Base
        {
            /// <summary>Base method</summary>
            public virtual void Method() { }
        }
        /// <summary>Derived class</summary>
        public class Derived : Base
        {
            /// <inheritdoc />
            public override void Method() { }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
    {
        { "comment_sense.allow_implicit_inheritdoc", "false" }
    };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingReturns()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public int {|CSENSE006:Method|}() => 0;
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <returns>TODO</returns>
            public int Method() => 0;
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingValueForProperty()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public int {|CSENSE014:Property|} { get; set; }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <value>TODO</value>
            public int Property { get; set; }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
    {
        { "comment_sense.visibility_level", "public" },
        { "dotnet_diagnostic.CSENSE014.severity", "warning" }
    };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingTypeParam()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public void Method<{|CSENSE004:T|}>() { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <typeparam name="T">TODO</typeparam>
            public void Method<T>() { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingParamInCorrectRelativePosition()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param name="a">A</param>
            /// <param name="c">C</param>
            public void Method(int a, int {|CSENSE002:b|}, int c) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param name="a">A</param>
            /// <param name="b">TODO</param>
            /// <param name="c">C</param>
            public void Method(int a, int b, int c) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingParamWithNamelessParamPresent()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param>Nameless</param>
            public void Method(int {|CSENSE002:a|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param name="a">TODO</param>
            /// <param>Nameless</param>
            public void Method(int a) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingValueWhenReturnsExists()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <returns>Stray</returns>
            public int {|CSENSE014:Property|} { get; set; }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <returns>Stray</returns>
            /// <value>TODO</value>
            public int Property { get; set; }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE013.severity"] = "none"
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMultipleMissingParamsFixAll()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public void Method(int {|CSENSE002:x|}, int {|CSENSE002:y|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <param name="x">TODO</param>
            /// <param name="y">TODO</param>
            public void Method(int x, int y) { }
        }
        """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task FixAllAcrossMultipleMembers()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            public void {|CSENSE001:Method1|}() { }

            public void {|CSENSE001:Method2|}() { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>TODO</summary>
            public void Method1() { }

            /// <summary>TODO</summary>
            public void Method2() { }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE001.severity"] = "warning"
        };

        await VerifyFixAllAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingSummaryBeforeAttribute()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            [System.Obsolete]
            public void {|CSENSE001:Method|}() { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>TODO</summary>
            [System.Obsolete]
            public void Method() { }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE001.severity"] = "warning"
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingParamToMultiLineDocumentation()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /**
             * <summary>Summary</summary>
             */
            public void Method(int {|CSENSE002:x|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /**
             * <summary>Summary</summary>
             * <param name="x">TODO</param>
             */
            public void Method(int x) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public void InsertInheritDocToExistingDocumentationManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// "),
            DocumentationSyntaxExtensions.CreateXmlElement(DocumentationTags.Summary, content: "S"),
            DocumentationSyntaxExtensions.CreateXmlText(Environment.NewLine)
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.InheritDoc, null, null);

        Assert.That(newTrivia.ToString(), Does.Contain("<inheritdoc />"));
    }

    [Test]
    public async Task AddMissingParamWithUnixLineEndings()
    {
        const string source = "/// <summary>S</summary>\npublic class Test\n{\n    /// <summary>S</summary>\n    public void Method(int {|CSENSE002:x|}) { }\n}";
        const string fixedSource = "/// <summary>S</summary>\npublic class Test\n{\n    /// <summary>S</summary>\n    /// <param name=\"x\">TODO</param>\n    public void Method(int x) { }\n}";

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingParamAtEndOfDocumentation()
    {
        const string source = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            public void Method(int {|CSENSE002:x|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <param name="x">TODO</param>
            public void Method(int x) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingSummaryBeforeMissingParam()
    {
        const string source = """
        /// <summary>Class</summary>
        public class Test
        {
            public void {|CSENSE001:Method|}(int x) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>TODO</summary>
            public void Method(int x) { }
        }
        """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE001.severity"] = "warning",
            ["dotnet_diagnostic.CSENSE002.severity"] = "none"
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingParamBeforeReturns()
    {
        const string source = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <returns>R</returns>
            public int Method(int {|CSENSE002:x|}) => 0;
        }
        """;
        const string fixedSource = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <param name="x">TODO</param>
            /// <returns>R</returns>
            public int Method(int x) => 0;
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingReturnsBeforeRemarks()
    {
        const string source = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <remarks>Rem</remarks>
            public int {|CSENSE006:Method|}() => 0;
        }
        """;
        const string fixedSource = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <returns>TODO</returns>
            /// <remarks>Rem</remarks>
            public int Method() => 0;
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingException()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public void {|CSENSE012:Method|}()
            {
                throw new System.ArgumentNullException();
            }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <exception cref="System.ArgumentNullException">TODO</exception>
            public void Method()
            {
                throw new System.ArgumentNullException();
            }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMultipleMissingExceptionsFixAll()
    {
        const string source = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            public void {|CSENSE012:{|CSENSE012:Method|}|}()
            {
                if (true) throw new System.ArgumentNullException();
                throw new System.InvalidOperationException();
            }
        }
        """;
        const string fixedSource = """
        /// <summary>Test class</summary>
        public class Test
        {
            /// <summary>Summary</summary>
            /// <exception cref="System.ArgumentNullException">TODO</exception>
            /// <exception cref="System.InvalidOperationException">TODO</exception>
            public void Method()
            {
                if (true) throw new System.ArgumentNullException();
                throw new System.InvalidOperationException();
            }
        }
        """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task AddMissingGenericException()
    {
        const string source = """
        public class MyException<T> : System.Exception { }

        public class Test
        {
            /// <summary>Summary</summary>
            /// <typeparam name="T">Type parameter</typeparam>
            public void {|CSENSE012:Method|}<T>()
            {
                throw new MyException<T>();
            }
        }
        """;
        const string fixedSource = """
            public class MyException<T> : System.Exception { }

            public class Test
            {
                /// <summary>Summary</summary>
                /// <typeparam name="T">Type parameter</typeparam>
                /// <exception cref="MyException{T}">TODO</exception>
                public void Method<T>()
                {
                    throw new MyException<T>();
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public void AddMissingParamToDetachedDocumentationManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// "),
            DocumentationSyntaxExtensions.CreateXmlElement(DocumentationTags.Summary, content: "S"),
            DocumentationSyntaxExtensions.CreateXmlText(Environment.NewLine)
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Param, "x", null, "TODO");

        Assert.That(newTrivia.ToString(), Does.Contain("<param name=\"x\">TODO</param>"));
    }

    [Test]
    public async Task AddMissingParamToDocumentationWithUnknownTag()
    {
        const string source = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <unknown>U</unknown>
            public void Method(int {|CSENSE002:x|}) { }
        }
        """;
        const string fixedSource = """
        /// <summary>Class</summary>
        public class Test
        {
            /// <summary>S</summary>
            /// <param name="x">TODO</param>
            /// <unknown>U</unknown>
            public void Method(int x) { }
        }
        """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public void AddMissingParamToDocumentationEndingWithoutNewlineManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// "),
            DocumentationSyntaxExtensions.CreateXmlElement(DocumentationTags.Summary, content: "S")
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Param, "x", null, "TODO");
        Assert.That(newTrivia.ToString().Trim(), Does.EndWith("<param name=\"x\">TODO</param>"));
    }

    [Test]
    public void AddMissingSummaryToBlockDocumentationManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.MultiLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/** ")
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Summary, null, null, "TODO");
        Assert.That(newTrivia.ToString(), Does.Contain("<summary>TODO</summary>"));
    }

    [Test]
    public void AddMissingSummaryToEmptyDocumentationManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>());

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Summary, null, null, "TODO");
        Assert.That(newTrivia.ToString(), Does.Contain("<summary>TODO</summary>"));
    }

    [Test]
    public void InsertTagToTriviaWithUnixNewLineManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("///\n/// ")
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Summary, null, null, "TODO");
        Assert.That(newTrivia.ToString(), Does.Contain("\n"));
        Assert.That(newTrivia.ToString(), Does.Not.Contain("\r\n"));
    }

    [Test]
    public void InsertTagToTriviaWithUnknownTagManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// "),
            DocumentationSyntaxExtensions.CreateXmlElement("unknown", content: "U"),
            DocumentationSyntaxExtensions.CreateXmlText(Environment.NewLine)
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, "newtag", null, null, "TODO");
        Assert.That(newTrivia.ToString(), Does.Contain("<newtag>TODO</newtag>"));
    }

    [Test]
    public void InsertTagImmediatelyAfterInitialPrefixManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// ")
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Summary, null, null, "TODO");

        // Should NOT contain a newline before <summary>
        Assert.That(newTrivia.ToString(), Is.EqualTo("/// <summary>TODO</summary>\r\n")
            .Or.EqualTo("/// <summary>TODO</summary>\n"));
    }

    [Test]
    public async Task AddMissingSummaryToExistingInvalidDocumentation()
    {
        const string source = """
            /// <summary>Test class</summary>
            public class Test
            {
                /// <unknown />
                public void {|CSENSE001:Method|}() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Test class</summary>
            public class Test
            {
                /// <summary>TODO</summary>
                /// <unknown />
                public void Method() { }
            }
            """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            ["dotnet_diagnostic.CSENSE001.severity"] = "warning"
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingInheritDocToExistingInvalidDocumentation()
    {
        const string source = """
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                public virtual void Method() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <unknown />
                public override void {|CSENSE018:Method|}() { }
            }
            """;
        const string fixedSource = """
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                public virtual void Method() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc />
                /// <unknown />
                public override void Method() { }
            }
            """;

        var options = new Dictionary<string, string>(DisableUnrelatedRules)
        {
            { "comment_sense.allow_implicit_inheritdoc", "false" }
        };

        await VerifyCodeFixAsync(source, fixedSource, options);
    }

    [Test]
    public async Task AddMissingParamToMultiLineDocumentationStyle2()
    {
        const string source = """
            /// <summary>Test class</summary>
            public class Test
            {
                /** <summary>Summary</summary> */
                public void Method(int {|CSENSE002:x|}) { }
            }
            """;
        const string fixedSource = """
            /// <summary>Test class</summary>
            public class Test
            {
                /** <summary>Summary</summary>
                 * <param name="x">TODO</param> */
                public void Method(int x) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public void FindInsertionIndexWithMultiLinePrefixManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.MultiLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/** ")
            ]));

        // This is a bit of a hack to call private static method via reflection if needed,
        // but InsertTagToTrivia calls FindInsertionIndex internally.

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Summary, null, null, "TODO");
        Assert.That(newTrivia.ToString(), Does.StartWith("/** <summary>TODO</summary>"));
    }
    [Test]
    public void InsertTagAfterNodeWithNewlineManual()
    {
        var newLine = Environment.NewLine;
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText("/// <summary>Summary</summary>" + newLine + "    /// ")
            ]));

        // insertionIndex should be at the end (1)
        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Param, "x", null, "TODO");

        // Since the previous node already contains a newline, it should NOT add another one.
        var s = newTrivia.ToString();
        Assert.That(s, Does.Not.Contain(newLine + newLine));
    }

    [Test]
    public void FindInsertionIndexWithoutStandardPrefixManual()
    {
        var trivia = SyntaxFactory.DocumentationCommentTrivia(
            SyntaxKind.SingleLineDocumentationCommentTrivia,
            SyntaxFactory.List<XmlNodeSyntax>([
                DocumentationSyntaxExtensions.CreateXmlText(" <summary>Summary</summary>") // Missing ///
            ]));

        var newTrivia = ContentGenerationCodeFixProvider.InsertTagToTrivia(trivia, DocumentationTags.Param, "x", null, "TODO");
        // Should be inserted at the end
        Assert.That(newTrivia.ToString(), Does.Contain("<param name=\"x\">TODO</param>"));
    }

    [Test]
    public void GetFixInternalAsyncWithInvalidScopeReturnsNull()
    {
        const FixAllScope invalidScope = (FixAllScope)(-1);
        var provider = (CodeFixProviderBase.FixAllProviderBase)new ContentGenerationCodeFixProvider().GetFixAllProvider();
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var result = provider.GetFixInternalAsync(invalidScope, null!);
        Assert.That(result, Is.Null);
    }
}
