using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class PrimaryConstructorTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task ClassPrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>Summary</summary>
            public class MyClass(int {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task ClassPrimaryConstructorDocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
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
            /// <summary>Summary</summary>
            public record MyRecord(int {|CSENSE002:p1|});
            """);
    }

    [Test]
    public async Task RecordPrimaryConstructorDocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public record MyRecord(int p1);
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RecordManualPropertyReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
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
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            /// <param name="p2">p2</param>
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
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public class MyClass(int p1, int {|CSENSE002:p2|})
            {
            }
            """);
    }

    [Test]
    public async Task StructPrimaryConstructorMissingParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>Summary</summary>
            public struct MyStruct(int {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task PrimaryConstructorWithRegularConstructorReportsDiagnosticsCorrectly()
    {
        await VerifyCSenseAsync("""
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public class MyClass(int p1)
            {
                /// <summary>Ctor</summary>
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
            /// <summary>Summary</summary>
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
            /// <summary>Summary</summary>
            /// <typeparam name="T">T</typeparam>
            public class MyClass<T>(T {|CSENSE002:p1|})
            {
            }
            """);
    }

    [Test]
    public async Task GenericTypePrimaryConstructorMissingTypeParameterDocumentationReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
            public class MyClass<{|CSENSE004:T|}>(int p1)
            {
            }
            """);
    }

    [Test]
    public async Task ClassMemberMatchingPrimaryConstructorParameterNameReportsDiagnostic()
    {
        await VerifyCSenseAsync("""
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
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
            /// <summary>Summary</summary>
            /// <param name="p1">p1</param>
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
            /// <summary>Summary</summary>
            /// <param name="p1">Primary constructor param</param>
            public class MyClass(int p1)
            {
                /// <summary>Method</summary>
                /// <param name="p2">Method param</param>
                public void DoSomething(int p2) { }

                /// <summary>Another Method</summary>
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
