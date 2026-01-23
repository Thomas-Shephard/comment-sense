using NUnit.Framework;

namespace CommentSense.Tests.CodeFixTests;

public class TypeParameterDocumentationCodeFixTests : CommentSenseCodeFixTestBase
{
    [Test]
    public async Task AddsTypeParamTagToClass()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            /// <typeparam name="T">Summary.</typeparam>
            public class MyClass<T>
            {
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task AddsTypeParamTagToMethod()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                public void MyMethod<{|CSENSE004:T|}>() { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            public class MyClass
            {
                /// <summary>Summary.</summary>
                /// <typeparam name="T">Summary.</typeparam>
                public void MyMethod<T>() { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task InsertsTypeParamTagInCorrectOrder()
    {
        const string testCode = """
            /// <summary>Summary.</summary>
            /// <typeparam name="T1">Summary.</typeparam>
            /// <typeparam name="T3">Summary.</typeparam>
            public class MyClass<T1, {|CSENSE004:T2|}, T3>
            {
                /// <summary>Summary.</summary>
                public void MyMethod() { }
            }
            """;

        const string fixedCode = """
            /// <summary>Summary.</summary>
            /// <typeparam name="T1">Summary.</typeparam>
            /// <typeparam name="T2">Summary.</typeparam>
            /// <typeparam name="T3">Summary.</typeparam>
            public class MyClass<T1, T2, T3>
            {
                /// <summary>Summary.</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}
