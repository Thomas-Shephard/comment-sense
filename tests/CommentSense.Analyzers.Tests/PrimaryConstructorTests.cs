using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class PrimaryConstructorTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task ClassPrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            public class MyClass(int {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task ClassPrimaryConstructorDocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass(int p1)
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RecordPrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>This is a summary for the record.</summary>
            public record MyRecord(int {|CSENSE002:p1|});
            """);
    }

    [Test]
    public async Task RecordPrimaryConstructorDocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>This is a summary for the record.</summary>
            /// <param name="p1">The first parameter.</param>
            public record MyRecord(int p1);
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RecordManualPropertyReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>This is a summary for the record.</summary>
            /// <param name="p1">The first parameter.</param>
            public record MyRecord(int p1)
            {
                public string {|CSENSE001:P2|} { get; init; }
            }
            """);
    }

    [Test]
    public async Task ClassPrimaryConstructorStrayParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            /// <param name="p2">The second parameter.</param>
            public class {|CSENSE003:MyClass|}(int p1)
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrimaryConstructorMixedDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass(int p1, int {|CSENSE002:p2|})
            {
            }
            """);
    }

    [Test]
    public async Task StructPrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the struct.</summary>
            public struct MyStruct(int {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task PrimaryConstructorWithRegularConstructorReportsDiagnosticsCorrectly()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass(int p1)
            {
                /// <summary>This is a summary for the constructor.</summary>
                public MyClass(int {|CSENSE002:p1|}, int {|CSENSE002:p2|}) : this(p1)
                {
                }
            }
            """);
    }

    [Test]
    public async Task InternalPrimaryConstructorDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            internal class MyClass(int p1)
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericTypePrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">The type parameter.</typeparam>
            public class MyClass<T>(T {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task GenericTypePrimaryConstructorMissingTypeParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass<{|CSENSE004:T|}>(int p1)
            {
            }
            """);
    }

    [Test]
    public async Task ClassMemberMatchingPrimaryConstructorParameterNameReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass(int p1)
            {
                public int {|CSENSE001:p1|};
            }
            """);
    }

    [Test]
    public async Task ClassMemberNotMatchingPrimaryConstructorParameterNameIsAnalyzed()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">The first parameter.</param>
            public class MyClass(int p1)
            {
                public int {|CSENSE001:p2|};
            }
            """);
    }

    [Test]
    public async Task DocumentationOnClassIsCorrectlyAppliedToPrimaryConstructorOnly()
    {
        await VerifyCSenseAsync("""
            /// <summary>This is a summary for the class.</summary>
            /// <param name="p1">Primary constructor param</param>
            public class MyClass(int p1)
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p2">Method param</param>
                public void DoSomething(int p2) { }

                /// <summary>This is a summary for another method.</summary>
                public void AnotherMethod(int {|CSENSE002:p3|}) { }
            }
            """, expectDiagnostic: true);
    }

    [Test]
    public async Task ClassWithPrimaryConstructorAndInheritdocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <inheritdoc />
            public class MyClass(int p1)
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
