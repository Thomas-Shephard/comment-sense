using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class GhostReferenceTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task GhostParameterInSummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Creates a new file with the specified {|CSENSE020:fileName|}.</summary>
                /// <param name="fileName">The name of the file.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GhostTypeParameterInSummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The type of {|CSENSE021:TValue|} to process.</summary>
                /// <typeparam name="TValue">The value type.</typeparam>
                public void Process<TValue>() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SafeModeIgnoresSimpleNames()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Pass the name here.</summary>
                /// <param name="name">The name.</param>
                public void Create(string name) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StrictModeFlagsSimpleNames()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Pass the {|CSENSE020:name|} here.</summary>
                /// <param name="name">The {|CSENSE020:name|}.</param>
                public void Create(string name) { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.ghost_references.mode", "Strict" }
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }

    [Test]
    public async Task CamelCaseIsFlaggedInSafeMode()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The {|CSENSE020:userId|} is required.</summary>
                /// <param name="userId">The ID.</param>
                public void Get(int userId) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnderscoreIsFlaggedInSafeMode()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Number of {|CSENSE020:max_retries|}.</summary>
                /// <param name="max_retries">The retries.</param>
                public void Set(int max_retries) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExactCaseConstraint()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The unique Id of the user.</summary>
                /// <param name="id">The ID.</param>
                public void Get(int id) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ShortWordFilterInSafeMode()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Index i, x, val, key should be ignored.</summary>
                /// <param name="i">The i.</param>
                /// <param name="x">The x.</param>
                /// <param name="val">The val.</param>
                /// <param name="key">The key.</param>
                public void Do(int i, int x, string val, string key) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task AlreadyWrappedInParamRefIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Creates a new file with the specified <paramref name="fileName"/>.</summary>
                /// <param name="fileName">The name.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task TypeParamAlreadyWrappedInTypeParamRefIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Processes <typeparamref name="TValue"/> items.</summary>
                /// <typeparam name="TValue">The type.</typeparam>
                public void Process<TValue>() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task OffModeDoesNothing()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Creates a new file with the specified fileName.</summary>
                /// <param name="fileName">The name.</param>
                public void Create(string fileName) { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.ghost_references.mode", "Off" }
        };

        await VerifyCSenseAsync(testCode, configOptions: config, expectDiagnostic: false);
    }

    [Test]
    public async Task SelfReferenceInParamTagIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>My method.</summary>
                /// <param name="fileName">The fileName of the file.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SelfReferenceInTypeParamTagIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>My method.</summary>
                /// <typeparam name="TValue">The TValue type.</typeparam>
                public void Process<TValue>() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task NestedTagInParamTagIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>My method.</summary>
                /// <param name="fileName">The <b>fileName</b> of the file.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ReferenceInsideCodeTagIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Example: <code>var x = fileName;</code></summary>
                /// <param name="fileName">The name.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ReferenceInsideSeeTagIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>See <see cref="Create(string)">fileName</see> for more info.</summary>
                /// <param name="fileName">The name.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task CustomTagWithNameAttributeIsProcessed()
    {
        // This test ensures that name attributes on custom tags are handled,
        // which typically hits the XmlTextAttributeSyntax path in GetNameAttributeValue.
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The <custom name="fileName">{|CSENSE020:fileName|}</custom> is here.</summary>
                /// <param name="fileName">The name.</param>
                public void Create(string fileName) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}