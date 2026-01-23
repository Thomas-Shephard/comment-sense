using NUnit.Framework;

namespace CommentSense.Tests.CodeFixTests;

public class ParameterDocumentationCodeFixTests : CommentSenseCodeFixTestBase
{
    [Test]
    public async Task AddsParamTagToExistingDocumentation()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                public void MyMethod(int {|CSENSE002:p|}) { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="p">Summary.</param>
                public void MyMethod(int p) { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task AddsMultipleParamTags()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="p1">Summary.</param>
                public void MyMethod(int p1, int {|CSENSE002:p2|}) { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="p1">Summary.</param>
                /// <param name="p2">Summary.</param>
                public void MyMethod(int p1, int p2) { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task InsertsParamTagInCorrectOrder()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="p1">Summary.</param>
                /// <param name="p3">Summary.</param>
                public void MyMethod(int p1, int {|CSENSE002:p2|}, int p3) { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <param name="p1">Summary.</param>
                /// <param name="p2">Summary.</param>
                /// <param name="p3">Summary.</param>
                public void MyMethod(int p1, int p2, int p3) { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}
