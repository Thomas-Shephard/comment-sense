using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class DiagnosticLocationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task SummaryDiagnosticPointsToTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// {|CSENSE016:<summary>Save</summary>|}
                public void Save() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ParameterDiagnosticPointsToTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// {|CSENSE016:<param name="p1">p1</param>|}
                public void MyMethod(int p1) { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReturnsDiagnosticPointsToTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// {|CSENSE016:<returns>returns</returns>|}
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueDiagnosticPointsToTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the property.</summary>
                /// {|CSENSE016:<value>value</value>|}
                public int MyProperty { get; set; }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionDiagnosticPointsToTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// {|CSENSE016:<exception cref="System.ArgumentNullException">ArgumentNullException</exception>|}
                public void MyMethod() { throw new System.ArgumentNullException(); }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateParamPointsToSecondTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <param name="p1">The first parameter.</param>
                /// {|CSENSE009:<param name="p1">The second parameter.</param>|}
                public void MyMethod(int p1) { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MultipleSummariesOnlyOneLowQualityPointsToCorrectTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a perfectly valid and descriptive summary documentation.</summary>
                /// {|CSENSE016:<summary>Save</summary>|}
                public void Save() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MultipleReturnsOnlyOneLowQualityPointsToCorrectTag()
    {
        const string testCode = """
            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <returns>This is a perfectly valid and descriptive return documentation.</returns>
                /// {|CSENSE016:<returns>returns</returns>|}
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// <exception cref="T:System.ArgumentException">Nested exception tag (should be ignored).</exception>
                /// </summary>
                /// {|CSENSE016:<exception cref="T:System.ArgumentNullException">ArgumentNullException</exception>|}
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SummaryLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <remarks>
                /// <summary>Nested summary (should be ignored).</summary>
                /// </remarks>
                /// {|CSENSE016:<summary>MyMethod</summary>|}
                public void MyMethod()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ParamLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// <param name="p1">Nested param (should be ignored).</param>
                /// </summary>
                /// {|CSENSE016:<param name="p1">p1</param>|}
                public void MyMethod(int p1)
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task TypeParamLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// <typeparam name="T">Nested typeparam (should be ignored).</typeparam>
                /// </summary>
                /// {|CSENSE016:<typeparam name="T">T</typeparam>|}
                public void MyMethod<T>()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReturnsLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// <returns>Nested returns (should be ignored).</returns>
                /// </summary>
                /// {|CSENSE016:<returns>return</returns>|}
                public int MyMethod() => 0;
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValueLocationMatchesWhenNestedTagsExist()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// Summary.
                /// <value>Nested value (should be ignored).</value>
                /// </summary>
                /// {|CSENSE016:<value>P</value>|}
                public int P { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
