using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class PropertySummaryPatternTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    private static readonly Dictionary<string, string> PropertyPatternConfig = new()
    {
        ["comment_sense.require_property_patterns"] = "true"
    };

    [Test]
    public async Task RequirePropertyPatternsDisabledByDefaultDoesNotReport()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// <summary>Wrong prefix text.</summary>
                public int Count { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GetOnlyPropertyRequiresGetsPrefix()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Returns the count.</summary>|}
                public int Count { get; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task SetOnlyPropertyRequiresSetsPrefix()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                private int _count;

                /// {|CSENSE027:<summary>Gets the count.</summary>|}
                public int Count { set => _count = value; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task GetAndSetPropertyRequiresGetsOrSetsPrefix()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets the count.</summary>|}
                public int Count { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task GetAndInitPropertyRequiresGetsOrInitializesPrefix()
    {
        const string testCode = """
            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit { }
            }

            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets or sets the count.</summary>|}
                public int Count { get; init; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task PublicPropertyWithPrivateSetterIsTreatedAsGetOnly()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets or sets the count.</summary>|}
                public int Count { get; private set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task BooleanPropertyRequiresValueIndicatingWhetherPhrase()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets or sets whether the operation is enabled.</summary>|}
                public bool IsEnabled { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task BooleanPropertyWithCorrectPhraseDoesNotReport()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// <summary>Gets or sets a value indicating whether the operation is enabled.</summary>
                public bool IsEnabled { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, expectDiagnostic: false, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task BooleanGetOnlyPropertyRequiresValueIndicatingWhetherPhrase()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets whether the operation is enabled.</summary>|}
                public bool IsEnabled { get; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task BooleanSetOnlyPropertyRequiresValueIndicatingWhetherPhrase()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                private bool _enabled;

                /// {|CSENSE027:<summary>Sets whether the operation is enabled.</summary>|}
                public bool IsEnabled { set => _enabled = value; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task BooleanInitPropertyRequiresGetsOrInitializesPhrase()
    {
        const string testCode = """
            namespace System.Runtime.CompilerServices
            {
                public sealed class IsExternalInit { }
            }

            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets or sets a value indicating whether the operation is enabled.</summary>|}
                public bool IsEnabled { get; init; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task SummaryStartingWithXmlTagIsHandled()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// <summary><see cref="System.String"/> Gets or sets the name.</summary>
                public string Name { get; set; } = string.Empty;
            }
            """;

        await VerifyPropertySummaryAsync(testCode, expectDiagnostic: false, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task CaseAndWhitespaceVariationsAreAccepted()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// <summary>gets   or   sets   the name.</summary>
                public string Name { get; set; } = string.Empty;
            }
            """;

        await VerifyPropertySummaryAsync(testCode, expectDiagnostic: false, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task MissingWhitespaceInsidePatternReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Getsor sets the name.</summary>|}
                public string Name { get; set; } = string.Empty;
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task PatternEndingBeforeExpectedWhitespaceReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets</summary>|}
                public int Count { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task PatternEndingBeforeExpectedWordReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>Gets </summary>|}
                public int Count { get; set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task GetOnlyPatternRequiresWordBoundary()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// {|CSENSE027:<summary>GetsValue for the current sample.</summary>|}
                public int Value { get; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, configOptions: PropertyPatternConfig);
    }

    [Test]
    public async Task PropertyWithLessVisibleSetterIsTreatedAsGetOnly()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class Sample
            {
                /// <summary>Gets the value.</summary>
                protected internal int Value { get; private set; }
            }
            """;

        await VerifyPropertySummaryAsync(testCode, expectDiagnostic: false, configOptions: PropertyPatternConfig);
    }

    private static Task VerifyPropertySummaryAsync(string source, bool expectDiagnostic = true, IDictionary<string, string>? configOptions = null)
    {
        return VerifyCSenseAsync(
            source,
            expectDiagnostic: expectDiagnostic,
            diagnosticOptions: [(CommentSenseDiagnosticIds.MissingValueDocumentationId, ReportDiagnostic.Suppress)],
            configOptions: configOptions);
    }
}
