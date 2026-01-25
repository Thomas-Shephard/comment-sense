using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class LowQualityDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task ParamRepeatingNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">p1</param>
                public void MyMethod(int {|CSENSE016:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ParamRepeatingNameCaseInsensitiveReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="parameterOne">parameterOne</param>
                public void MyMethod(int {|CSENSE016:parameterOne|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TypeParamRepeatingNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">T</typeparam>
            public class MyClass<{|CSENSE016:T|}>
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReturnsRepeatingReturnsKeywordReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>returns</returns>
                public int {|CSENSE016:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReturnsRepeatingReturnKeywordReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>return</returns>
                public int {|CSENSE016:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReturnsRepeatingTypeNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns>Int32</returns>
                public int {|CSENSE016:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueRepeatingValueKeywordReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>value</value>
                public int {|CSENSE016:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueRepeatingTypeNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Int32</value>
                public int {|CSENSE016:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionRepeatingTypeNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="System.ArgumentNullException">ArgumentNullException</exception>
                public void {|CSENSE016:MyMethod|}()
                {
                    throw new System.ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GoodDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// <typeparam name="T">The type of elements.</typeparam>
            public class MyClass<T>
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                /// <returns>The result of the operation.</returns>
                public int MyMethod(int p1) => p1;

                /// <summary>This is a summary for the property.</summary>
                /// <value>The count of items.</value>
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task EmptyParamReportsDiagnostic()
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
    public async Task WhitespaceParamReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="p1">   </param>
                public void MyMethod(int {|CSENSE016:p1|}) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyTypeParamReportsDiagnostic()
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
    public async Task EmptyReturnsReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <returns></returns>
                public int {|CSENSE016:MyMethod|}() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyValueReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value></value>
                public int {|CSENSE016:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyExceptionReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="System.ArgumentNullException"></exception>
                public void {|CSENSE016:MyMethod|}()
                {
                    throw new System.ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
