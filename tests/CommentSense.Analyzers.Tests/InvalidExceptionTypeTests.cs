using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InvalidExceptionTypeTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task NonExceptionTypeReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE017:string|}">This is not an exception.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValidExceptionTypeDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="ArgumentNullException">This is a valid exception.</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task CustomExceptionTypeDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the exception.</summary>
            public class MyException : Exception { }

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="MyException">This is a valid custom exception.</exception>
                public void MyMethod()
                {
                    throw new MyException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SelfClosingExceptionTagWithInvalidTypeReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE017:string|}"/>
                public void {|CSENSE012:{|CSENSE016:MyMethod|}|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task OtherTagsDoNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>
                /// This is a summary for the method with a see tag <see cref="string"/>.
                /// </summary>
                public void MyMethod()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MethodReferenceInExceptionTagReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE017:MyMethod|}">Reference to a method, not a type.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        // CSENSE012 is reported because the exception is not documented properly (cref is not an exception type).
        // CSENSE017 is reported because "MyMethod" resolves to a method, not a type.
        await VerifyCSenseAsync(testCode);
    }
}
