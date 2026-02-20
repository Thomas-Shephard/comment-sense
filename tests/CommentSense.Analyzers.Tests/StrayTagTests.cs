using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class StrayTagTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task NestedSummaryIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <remarks>
                /// {|CSENSE022:<summary>Nested summary</summary>|}
                /// </remarks>
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateSummaryIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>First summary</summary>
                /// {|CSENSE022:<summary>Second summary</summary>|}
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedExceptionIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE023:<exception cref="System.Exception">Nested exception</exception>|}
                /// </summary>
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedExceptionWithoutCrefIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE023:<exception>Nested exception without cref</exception>|}
                /// </summary>
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateExceptionIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <exception cref="System.Exception">First</exception>
                /// {|CSENSE023:<exception cref="System.Exception">Second</exception>|}
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedParamIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE003:<param name="p">Nested param</param>|}
                /// </summary>
                /// <param name="p">Valid param</param>
                public void MyMethod(int p) { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedReturnsIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE013:<returns>Nested returns</returns>|}
                /// </summary>
                /// <returns>Valid returns</returns>
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateReturnsIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <returns>First</returns>
                /// {|CSENSE013:<returns>Second</returns>|}
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedValueIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE015:<value>Nested value</value>|}
                /// </summary>
                /// <value>Valid value</value>
                public int MyProperty { get; set; }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateExceptionWithDifferentCrefFormatIsFlaggedAsStrayUsingCref()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <exception cref="System.Exception">First</exception>
                /// {|CSENSE023:<exception cref="T:System.Exception">Second</exception>|}
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NestedParamWithoutNameIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>
                /// {|CSENSE003:<param>Nested param without name</param>|}
                /// </summary>
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateUnresolvedExceptionIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <exception cref="T:UnknownException">First</exception>
                /// {|CSENSE023:<exception cref="T:UnknownException">Second</exception>|}
                public void MyMethod() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task StrayReturnsOnPropertyIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// {|CSENSE013:<returns>Stray returns on property</returns>|}
                /// <value>The value of the property.</value>
                public int MyProperty { get; set; }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateReturnsOnMethodIsFlaggedAsStray()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <returns>First</returns>
                /// {|CSENSE013:<returns>Second</returns>|}
                public int MyMethod() => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }
}
