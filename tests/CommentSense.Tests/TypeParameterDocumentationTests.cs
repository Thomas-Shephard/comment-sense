using NUnit.Framework;

namespace CommentSense.Tests;

public class TypeParameterDocumentationTests : CommentSenseTestBase
{
    [Test]
    public async Task MissingTypeParameterDocumentationOnClassReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MissingTypeParameterDocumentationOnMethodReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public void MyMethod<{|CSENSE004:T|}>() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedTypeParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <typeparam name="T">Type T</typeparam>
            public class MyClass<T>
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StrayTypeParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <typeparam name="T">Type T</typeparam>
            public class {|CSENSE005:MyClass|}
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MultipleMissingTypeParameterDocumentationsReportDiagnostics()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass<{|CSENSE004:T1|}, {|CSENSE004:T2|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PartiallyMissingTypeParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <typeparam name="T1">T1</typeparam>
            public class MyClass<T1, {|CSENSE004:T2|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyTypeParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <typeparam name="T"></typeparam>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithNoTypeParametersDoesNotReportDiagnostics()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base</summary>
            /// <typeparam name="T">T</typeparam>
            public class Base<T> { }

            /// <inheritdoc />
            public class Derived<T> : Base<T> { }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MethodWithBothParametersAndTypeParametersCorrectlyDocumentedDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            public class MyClass
            {
                /// <summary>Summary</summary>
                /// <typeparam name="T">Type T</typeparam>
                /// <param name="p">Param P</param>
                public void MyMethod<T>(int p) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task TypeParameterWithMissingNameAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <typeparam>Missing name attribute</typeparam>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
