using NUnit.Framework;

namespace CommentSense.Tests.CodeFixTests;

public class MissingDocumentationCodeFixTests : CommentSenseCodeFixTestBase
{
    [Test]
    public async Task AddsSummaryToClass()
    {
        const string testCode = """
            public class [|MyClass|]
            {
            }
            """;

        const string fixedCode = """
            /// <summary>
            /// Summary.
            /// </summary>
            public class MyClass
            {
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task AddsSummaryToMethod()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                public void [|MyMethod|]() { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// </summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}