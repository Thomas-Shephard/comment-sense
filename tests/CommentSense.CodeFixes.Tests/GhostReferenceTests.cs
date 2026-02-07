using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class GhostReferenceTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, GhostReferenceCodeFixProvider>
{
    private static readonly Dictionary<string, string> DisableUnrelatedRules = new()
    {
        { "dotnet_diagnostic.CSENSE001.severity", "none" },
        { "dotnet_diagnostic.CSENSE002.severity", "none" },
        { "dotnet_diagnostic.CSENSE004.severity", "none" },
        { "dotnet_diagnostic.CSENSE006.severity", "none" }
    };

    [Test]
    public async Task WrapParameterInSummary()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Processes {|CSENSE020:inputData|}.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Processes <paramref name="inputData" />.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task WrapParameterWithDifferentCasing()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Processes {|CSENSE020:Inputdata|}.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Processes <paramref name="inputData" />.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task WrapTypeParameterInSummary()
    {
        const string source = """
            /// <summary>
            /// A collection of {|CSENSE021:TValue|}.
            /// </summary>
            public class Test<TValue>
            {
            }
            """;
        const string fixedSource = """
            /// <summary>
            /// A collection of <typeparamref name="TValue" />.
            /// </summary>
            public class Test<TValue>
            {
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task WrapMultipleReferencesInBatch()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Takes {|CSENSE020:firstValue|} and {|CSENSE020:secondValue|}.
                /// </summary>
                public void Add(int firstValue, int secondValue) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Takes <paramref name="firstValue" /> and <paramref name="secondValue" />.
                /// </summary>
                public void Add(int firstValue, int secondValue) { }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task CodeActionTitleUsesOriginalName()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Processes {|CSENSE020:Inputdata|}.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Processes <paramref name="inputData" />.
                /// </summary>
                public void Process(string inputData) { }
            }
            """;

        await VerifyCodeFixTitleAsync(source, fixedSource, "Wrap in <paramref name=\"inputData\" />", DisableUnrelatedRules);
    }

    [Test]
    public async Task WrapRegularConstructorParameter()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// A constructor that takes {|CSENSE020:inputParamValue|}.
                /// </summary>
                public Test(int inputParamValue)
                {
                }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// A constructor that takes <paramref name="inputParamValue" />.
                /// </summary>
                public Test(int inputParamValue)
                {
                }
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task WrapPrimaryConstructorParameter()
    {
        const string source = """
            /// <summary>
            /// A class that takes {|CSENSE020:inputParamValue|}.
            /// </summary>
            public class Test(int inputParamValue)
            {
            }
            """;
        const string fixedSource = """
            /// <summary>
            /// A class that takes <paramref name="inputParamValue" />.
            /// </summary>
            public class Test(int inputParamValue)
            {
            }
            """;

        await VerifyCodeFixAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task MultipleReferencesInSameSummaryFixAll()
    {
        const string source = """
            public class Test
            {
                /// <summary>
                /// Uses {|CSENSE020:FirstValue|} and {|CSENSE020:SecondValue|} to do {|CSENSE020:StuffToDo|}.
                /// </summary>
                public void Do(int FirstValue, int SecondValue, string StuffToDo) { }
            }
            """;
        const string fixedSource = """
            public class Test
            {
                /// <summary>
                /// Uses <paramref name="FirstValue" /> and <paramref name="SecondValue" /> to do <paramref name="StuffToDo" />.
                /// </summary>
                public void Do(int FirstValue, int SecondValue, string StuffToDo) { }
            }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }

    [Test]
    public async Task MixedGhostReferencesFixAll()
    {
        const string source = """
            /// <summary>
            /// A {|CSENSE021:TValue|} collection that uses {|CSENSE020:initialSize|}.
            /// </summary>
            public class MyCollection<TValue>(int initialSize) { }
            """;
        const string fixedSource = """
            /// <summary>
            /// A <typeparamref name="TValue" /> collection that uses <paramref name="initialSize" />.
            /// </summary>
            public class MyCollection<TValue>(int initialSize) { }
            """;

        await VerifyFixAllAsync(source, fixedSource, DisableUnrelatedRules);
    }
}
