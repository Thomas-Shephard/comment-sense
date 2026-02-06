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
}
