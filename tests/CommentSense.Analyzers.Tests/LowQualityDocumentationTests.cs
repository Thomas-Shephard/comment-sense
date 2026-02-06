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
                /// {|CSENSE016:<param name="p1">p1</param>|}
                public void MyMethod(int p1) { }
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
                /// {|CSENSE016:<param name="parameterOne">parameterOne</param>|}
                public void MyMethod(int parameterOne) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TypeParamRepeatingNameReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// {|CSENSE016:<typeparam name="T">T</typeparam>|}
            public class MyClass<T>
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
                /// {|CSENSE016:<returns>returns</returns>|}
                public int MyMethod() => 0;
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
                /// {|CSENSE016:<returns>return</returns>|}
                public int MyMethod() => 0;
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
                /// {|CSENSE016:<returns>Int32</returns>|}
                public int MyMethod() => 0;
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
                /// {|CSENSE016:<value>value</value>|}
                public int MyProperty { get; set; }
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
                /// {|CSENSE016:<value>Int32</value>|}
                public int MyProperty { get; set; }
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
                /// {|CSENSE016:<exception cref="System.ArgumentNullException">ArgumentNullException</exception>|}
                public void MyMethod()
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
                /// {|CSENSE016:<param name="p1"></param>|}
                public void MyMethod(int p1) { }
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
                /// {|CSENSE016:<param name="p1">   </param>|}
                public void MyMethod(int p1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task EmptyTypeParamReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            /// {|CSENSE016:<typeparam name="T"></typeparam>|}
            public class MyClass<T>
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
                /// {|CSENSE016:<returns></returns>|}
                public int MyMethod() => 0;
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
                /// {|CSENSE016:<value></value>|}
                public int MyProperty { get; set; }
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
                /// {|CSENSE016:<exception cref="System.ArgumentNullException"></exception>|}
                public void MyMethod()
                {
                    throw new System.ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GenericListReturnDocumentationWithOnlyTypeNameIsFlagged()
    {
        const string testCode = """
            using System.Collections.Generic;
            /// <summary>Class summary</summary>
            public class MyClass
            {
                /// <summary>Method summary that is long enough.</summary>
                /// {|CSENSE016:<returns>List</returns>|}
                public List<int> GetItems() => null;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GenericListReturnDocumentationWithFullTypeNameIsFlagged()
    {
        const string testCode = """
            using System.Collections.Generic;
            /// <summary>Class summary</summary>
            public class MyClass
            {
                /// <summary>Method summary that is long enough.</summary>
                /// {|CSENSE016:<returns>List&lt;int&gt;</returns>|}
                public List<int> GetItems() => null;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
