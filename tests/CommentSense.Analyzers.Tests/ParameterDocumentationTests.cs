using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ParameterDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod(int {|CSENSE002:param1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedParameterDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="param1">The first parameter.</param>
                public void MyMethod(int param1) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StrayParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="param1">The first parameter.</param>
                /// <param name="param2">The second parameter.</param>
                public void {|CSENSE003:MyMethod|}(int param1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MethodWithNoParametersDoesNotReportParameterDiagnostics()
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
    public async Task MultipleMissingParameterDocumentationsReportDiagnostics()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod(int {|CSENSE002:p1|}, string {|CSENSE002:p2|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConstructorWithMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the constructor.</summary>
                public MyClass(int {|CSENSE002:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PartiallyMissingParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                public void MyMethod(int p1, int {|CSENSE002:p2|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1"></param>
                public void MyMethod(int {|CSENSE016:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is the summary for the base class.</summary>
            public class Base
            {
                /// <summary>This is the summary for the base method.</summary>
                /// <param name="p">The input value.</param>
                public virtual void M(int p) { }
            }

            /// <summary>This is the summary for the derived class.</summary>
            public class Derived : Base
            {
                /// <inheritdoc />
                public override void M(int p) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IncludedDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <include file='docs.xml' path='[@name="test"]'/>
                public void M(int p) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ParameterOrderMismatchReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p2">The second parameter.</param>
                /// <param name="p1">The first parameter.</param>
                public void {|CSENSE008:MyMethod|}(int p1, int p2) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateParameterDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                /// <param name="p1">The duplicated first parameter.</param>
                public void {|CSENSE009:MyMethod|}(int p1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ParameterTagWithMissingNameAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param>Missing name attribute</param>
                public void MyMethod(int {|CSENSE002:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ParameterTagWithWhitespaceNameAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name=" ">Whitespace name attribute</param>
                public void MyMethod(int {|CSENSE002:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CorrectParameterOrderDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                /// <param name="p2">The second parameter.</param>
                public void MyMethod(int p1, int p2) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DuplicateParameterNamesInSignatureDoesNotCrash()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                public void MyMethod(int p1, int p1) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, compilerDiagnostics: CompilerDiagnostics.None);
    }
}
