using NUnit.Framework;

namespace CommentSense.Tests.CodeFixTests;

public class StrayDocumentationCodeFixTests : CommentSenseCodeFixTestBase
{
    [Test]
    public async Task RemovesStrayParamTag()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="stray">Summary.</param>
                public void {|CSENSE003:MyMethod|}() { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task RemovesStrayTypeParamTag()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <typeparam name="TStray">Summary.</typeparam>
                public void {|CSENSE005:MyMethod|}() { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}
