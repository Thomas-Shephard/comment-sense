using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.CodeFixes;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class RedundancyRemovalTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, RedundancyRemovalCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE016.severity", "none" },
        { "dotnet_diagnostic.CSENSE024.severity", "none" }
    };

    [Test]
    public async Task RemoveStrayParameterDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray">Stray</param>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveDuplicateParameterDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="x">First</param>
                /// {|CSENSE009:<param name="x">Second</param>|}
                public void Method(int x) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// <param name="x">First</param>
                public void Method(int x) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayTypeParameterDocumentation()
    {
        const string source = """
            /// <summary>Summary</summary>
            /// {|CSENSE005:<typeparam name="T">Stray</typeparam>|}
            public class Test { }
            """;
        const string fixedSource = """
            /// <summary>Summary</summary>
            public class Test { }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveDuplicateTypeParameterDocumentation()
    {
        const string source = """
            /// <summary>Summary</summary>
            /// <typeparam name="T">First</typeparam>
            /// {|CSENSE011:<typeparam name="T">Second</typeparam>|}
            public class Test<T> { }
            """;
        const string fixedSource = """
            /// <summary>Summary</summary>
            /// <typeparam name="T">First</typeparam>
            public class Test<T> { }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayReturnValueOnVoidMethod()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE013:<returns>Something</returns>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayValueOnMethod()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE015:<value>Something</value>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveMultipleStrayTagsFixAll()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray1">Stray</param>|}
                /// {|CSENSE003:<param name="stray2">Stray</param>|}
                /// {|CSENSE015:<value>Stray</value>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveEmptyElementStrayParameter()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray" />|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayParameterAsFirstNode()
    {
        const string source = """
            public class Test
            {
                /// {|CSENSE003:<param name="stray">Stray</param>|}
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveRedundantTagWithNoWhitespace()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>{|CSENSE015:<value>Stray</value>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task CodeActionTitleForUnnamedTag()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE013:<returns>Something</returns>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixTitleAsync(source, fixedSource, "Remove redundant <returns />", DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveRedundancyInMultipleDocumentsFixAll()
    {
        var test = new Microsoft.CodeAnalysis.CSharp.Testing.CSharpCodeFixTest<CommentSenseAnalyzer, RedundancyRemovalCodeFixProvider, NUnitVerifier>
        {
            TestState =
            {
                Sources =
                {
                    """
                    public class Test1
                    {
                        /// <summary>Summary</summary>
                        /// {|CSENSE003:<param name="stray">Stray</param>|}
                        public void Method() { }
                    }
                    """,
                    """
                    public class Test2
                    {
                        /// <summary>Summary</summary>
                        /// {|CSENSE015:<value>Stray</value>|}
                        public void Method() { }
                    }
                    """
                }
            },
            FixedState =
            {
                Sources =
                {
                    """
                    public class Test1
                    {
                        /// <summary>Summary</summary>
                        public void Method() { }
                    }
                    """,
                    """
                    public class Test2
                    {
                        /// <summary>Summary</summary>
                        public void Method() { }
                    }
                    """
                }
            }
        };

        test.ApplyCommonConfiguration(DisableUnrelatedRules, Microsoft.CodeAnalysis.DocumentationMode.Parse, null);

        await test.RunAsync();
        Assert.That(test.FixedState.Sources, Has.Count.EqualTo(2));
    }

    [Test]
    public async Task CodeActionTitleIncludesName()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray">Stray</param>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixTitleAsync(source, fixedSource, "Remove redundant <param name=\"stray\" />", DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveRedundancyInMultipleTriviasFixAll()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray1">Stray</param>|}
                public void Method1() { }

                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray2">Stray</param>|}
                public void Method2() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method1() { }

                /// <summary>Summary</summary>
                public void Method2() { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task GetAssociatedWhitespaceToRemoveNoWhitespaceReturnsNull()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>{|CSENSE013:<returns>Value</returns>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task GetAssociatedWhitespaceToRemoveTrailingWhitespaceButNotLastNodeRemovesTrailing()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE003:<param name="stray">Stray</param>|}///
                /// <returns>Value</returns>
                public int Method() => 0;
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>///
                /// <returns>Value</returns>
                public int Method() => 0;
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task GetNameAttributeEmptyElementWithoutNameReturnsNull()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE013:<returns />|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task IsPureWhitespaceOrPrefixNotPureReturnsFalse()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// text{|CSENSE003:<param name="x">Stray</param>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// text
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task IsPureWhitespaceOrPrefixEmptyStringReturnsTrue()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>{|CSENSE013:<returns></returns>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayTagNestedInAnotherTag()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// {|CSENSE013:<returns>Stray</returns>|}
                /// </summary>
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// </summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayTagAtEndOfContent()
    {
        const string source = """
            public class Test
            {
                /// <summary>Summary</summary>
                /// {|CSENSE013:<returns>Stray</returns>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>Summary</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStraySummaryDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <remarks>
                /// {|CSENSE022:<summary>Nested</summary>|}
                /// </remarks>
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <remarks>
                /// </remarks>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveDuplicateSummaryDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <summary>First</summary>
                /// {|CSENSE022:<summary>Second</summary>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>First</summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveStrayExceptionDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// {|CSENSE023:<exception cref="System.Exception">Nested</exception>|}
                /// </summary>
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// </summary>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task RemoveDuplicateExceptionDocumentation()
    {
        const string source = """
            public class Test
            {
                /// <exception cref="System.Exception">First</exception>
                /// {|CSENSE023:<exception cref="System.Exception">Second</exception>|}
                public void Method() { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <exception cref="System.Exception">First</exception>
                public void Method() { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public void GetFixInternalAsyncWithInvalidScopeReturnsNull()
    {
        const FixAllScope invalidScope = (FixAllScope)(-1);
        var provider = (CodeFixProviderBase.FixAllProviderBase)new RedundancyRemovalCodeFixProvider().GetFixAllProvider();
        // ReSharper disable once NullableWarningSuppressionIsUsed
        var result = provider.GetFixInternalAsync(invalidScope, null!);
        Assert.That(result, Is.Null);
    }
}
