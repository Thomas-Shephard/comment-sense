using Microsoft.CodeAnalysis.CSharp.Testing;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Tests;

public class CommentSenseAnalyzerTests
{
    [Test]
    public async Task PublicClassWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            public class [|MyClass|]
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMethodWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                public void [|MyMethod|]() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrivateClassWithoutDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            internal class MyClass
            {
                private void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DocumentedPublicClassDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>My summary</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PublicFieldWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                public int [|MyField|];
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicPropertyWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                public int [|MyProperty|] { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicEventWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>Summary</summary>
            public class MyClass
            {
                public event EventHandler [|MyEvent|];
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicClassWithMultipleTagsDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <remarks>Remarks</remarks>
            public class MyClass { }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    private static async Task VerifyCSenseAsync(string source, bool expectDiagnostic = true)
    {
        var tester = new CSharpAnalyzerTest<CommentSenseAnalyzer, NUnitVerifier>
        {
            TestCode = source,
            MarkupOptions = MarkupOptions.UseFirstDescriptor
        };

        switch (expectDiagnostic)
        {
            case false when source.Contains("[|"):
                Assert.Fail("Test code contains diagnostic markers [| |] but expectDiagnostic is false.");
                break;
            case true when !source.Contains("[|"):
                Assert.Fail("expectDiagnostic is true but test code contains no diagnostic markers [| |].");
                break;
        }

        await tester.RunAsync();
    }
}
