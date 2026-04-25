using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
using CommentSense.Analyzers.Logic;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class GhostReferenceTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public void NameListComparerWorksCorrectly()
    {
        var comparer = new GhostReferenceAnalyzer.NameListComparer();

        var array1 = ImmutableArray.Create("a", "b");
        var array2 = ImmutableArray.Create("a", "b");
        var array3 = ImmutableArray.Create("a", "B");
        var array4 = ImmutableArray.Create("a");
        var array5 = ImmutableArray.Create("a", "c");

        using (Assert.EnterMultipleScope())
        {
            // Equal arrays
            Assert.That(comparer.Equals(array1, array2), Is.True);
            Assert.That(comparer.GetHashCode(array1), Is.EqualTo(comparer.GetHashCode(array2)));

            // Case-insensitive equal
            Assert.That(comparer.Equals(array1, array3), Is.True);
            Assert.That(comparer.GetHashCode(array1), Is.EqualTo(comparer.GetHashCode(array3)));

            // Different length
            Assert.That(comparer.Equals(array1, array4), Is.False);

            // Different content
            Assert.That(comparer.Equals(array1, array5), Is.False);
        }
    }

    [Test]
    public async Task FastPathDetectsGhostReferencesWithManyParameters()
    {
        var parameterCount = 110;
        var parameters = string.Join(", ", Enumerable.Range(0, parameterCount).Select(i => $"int p{i}"));

        var code = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// This summary mentions {|CSENSE020:p0|} and {|CSENSE020:p{{parameterCount - 1}}|}.
                /// </summary>
                public void M({{parameters}}) { }
            }
            """;

        await VerifyCSenseAsync(code, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathDetectsGhostReferencesWithLongText()
    {
        var longSummary = new string('a', 50001) + " {|CSENSE020:p0|} " + new string('b', 1000);

        var code = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// {{longSummary}}
                /// </summary>
                public void M(int p0) { }
            }
            """;

        await VerifyCSenseAsync(code, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task RegexCacheEvictionPathIsExercised()
    {
        const int methodCount = 270;
        var methods = string.Join(Environment.NewLine, Enumerable.Range(0, methodCount).Select(i => $$"""
                    /// <summary>Handles {|CSENSE020:p{{i}}|} correctly.</summary>
                    /// <param name="p{{i}}">The value.</param>
                    public void M{{i}}(int p{{i}}) { }
            """));

        var code = $$"""
            using System;
            namespace Test;
            public class C {
            {{methods}}
            }
            """;

        await VerifyCSenseAsync(code, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathHandlesCaseVariantParametersCorrectly()
    {
        var padding = new string(' ', 50001);
        var testCode = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// {{padding}}
                /// The {|#0:ID1|} and the {|#1:id1|}.
                /// </summary>
                public void Get(int id1, int ID1) { }
            }
            """;

        var expected1 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("ID1", "ID1");
        var expected2 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(1)
            .WithArguments("id1", "id1");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected1, expected2], diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathWithManyParametersHandlesCaseVariants()
    {
        var parameterCount = 102;
        var parameters = string.Join(", ", Enumerable.Range(1, parameterCount - 2).Select(i => $"int p{i}"));
        parameters = "int p0, int P0, " + parameters;

        var testCode = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// The {|#0:p0|} and the {|#1:P0|}.
                /// </summary>
                public void M({{parameters}}) { }
            }
            """;

        var expected1 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("p0", "p0");
        var expected2 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(1)
            .WithArguments("P0", "P0");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected1, expected2], diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathIgnoresSelfReferenceInParamTag()
    {
        var padding = new string(' ', 50001);
        var testCode = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>Summary</summary>
                /// <param name="p0">{{padding}} The p0 parameter.</param>
                public void M(int p0) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathPreventsDuplicateDiagnosticsForSameSpan()
    {
        var padding = new string(' ', 50001);

        var testCode = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// {{padding}}
                /// The {|CSENSE020:T|} mention.
                /// </summary>
                public void M<T>(int T) { }
            }
            """;

        await VerifyCSenseAsync(testCode, compilerDiagnostics: CompilerDiagnostics.None, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingTypeParameterDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task FastPathHandlesUnicodeIdentifiersCorrectly()
    {
        var padding = new string(' ', 50001);

        var testCode = $$"""
            using System;
            namespace Test;
            public class C {
                /// <summary>
                /// {{padding}}
                /// The {|CSENSE020:λ_identifier|} mention.
                /// </summary>
                public void M(int λ_identifier) { }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions:
        [
            (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.MissingParameterDocumentationId, ReportDiagnostic.Suppress),
            (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
        ]);
    }

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
    public async Task CaseInsensitiveConstraint()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The unique {|#0:Id|} of the user.</summary>
                /// <param name="id">The ID.</param>
                public void Get(int id) { }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("Id", "id");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task CaseInsensitiveMatchForParameterInSafeMode()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The {|#0:myparam|} value.</summary>
                /// <param name="MyParam">The parameter.</param>
                public void Get(int MyParam) { }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("myparam", "MyParam");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task PreferExactMatchWhenMultipleParametersDifferByCase()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The {|#0:ID1|} and the {|#1:id1|}.</summary>
                /// <param name="id1">The id.</param>
                /// <param name="ID1">The ID.</param>
                public void Get(int id1, int ID1) { }
            }
            """;

        var expected1 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("ID1", "ID1");
        var expected2 = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(1)
            .WithArguments("id1", "id1");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected1, expected2]);
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
    public async Task SelfReferenceInParamTagWithDifferentCasingIsIgnored()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>My method.</summary>
                /// <param name="id">The ID of the user.</param>
                public void Get(int id) { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GhostReferenceToOtherParameterWithSameNameDifferentCase()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The method.</summary>
                /// <param name="id1">The {|#0:ID1|} value.</param>
                /// <param name="ID1">The ID.</param>
                public void Get(int id1, int ID1) { }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.GhostParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("ID1", "ID1");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
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

    [Test]
    [SuppressMessage("Performance", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
    public void AddRegexToCacheHandlesExistingEntry()
    {
        var names = ImmutableArray.Create("UniqueParameterName_" + Guid.NewGuid().ToString("N"));
        var regex1 = new Regex("pattern1");
        var regex2 = new Regex("pattern2");

        var result1 = GhostReferenceAnalyzer.AddRegexToCache(names, regex1);
        var result2 = GhostReferenceAnalyzer.AddRegexToCache(names, regex2);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result1, Is.SameAs(regex1));
            Assert.That(result2, Is.SameAs(regex1));
        }
    }

    [Test]
    public async Task GhostTypeParameterWithDifferentCasingIsFlagged()
    {
        const string testCode = """
            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>The type of {|#0:tvalue|} to process.</summary>
                /// <typeparam name="TValue">The value type.</typeparam>
                public void Process<TValue>() { }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.GhostTypeParameterReferenceRule)
            .WithLocation(0)
            .WithArguments("tvalue", "TValue");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }
}
