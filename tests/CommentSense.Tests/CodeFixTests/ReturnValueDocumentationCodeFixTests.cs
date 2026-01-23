using NUnit.Framework;

namespace CommentSense.Tests.CodeFixTests;

public class ReturnValueDocumentationCodeFixTests : CommentSenseCodeFixTestBase
{
    [Test]
    public async Task AddsReturnsTagToMethod()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                public int {|CSENSE006:MyMethod|}() => 0;
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <returns>Summary.</returns>
                public int MyMethod() => 0;
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}
