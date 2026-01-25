using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class TypeParameterDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MissingTypeParameterDocumentationOnClassReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
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
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod<{|CSENSE004:T|}>() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedTypeParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">The type parameter T.</typeparam>
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
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">The type parameter T.</typeparam>
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
            /// <summary>This is a summary for the class.</summary>
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
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T1">The first type parameter.</typeparam>
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
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T"></typeparam>
            public class MyClass<{|CSENSE016:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithNoTypeParametersDoesNotReportDiagnostics()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the base class.</summary>
            /// <typeparam name="T">The type parameter T.</typeparam>
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
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <typeparam name="T">The type parameter T.</typeparam>
                /// <param name="p">The parameter P.</param>
                public void MyMethod<T>(int p) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task TypeParameterWithMissingNameAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam>Missing name attribute</typeparam>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TypeParameterWithWhitespaceNameAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name=" ">Whitespace name attribute</typeparam>
            public class MyClass<{|CSENSE004:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TypeParameterOrderMismatchReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T2">The second type parameter.</typeparam>
            /// <typeparam name="T1">The first type parameter.</typeparam>
            public class {|CSENSE010:MyClass|}<T1, T2>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateTypeParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T1">The first type parameter.</typeparam>
            /// <typeparam name="T1">The duplicated first type parameter.</typeparam>
            public class {|CSENSE011:MyClass|}<T1>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CorrectTypeParameterOrderDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T1">The first type parameter.</typeparam>
            /// <typeparam name="T2">The second type parameter.</typeparam>
            public class MyClass<T1, T2>
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DuplicateTypeParameterNamesInSignatureDoesNotCrash()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">The type parameter.</typeparam>
            public class MyClass<T, T>
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, compilerDiagnostics: CompilerDiagnostics.None);
    }
}
