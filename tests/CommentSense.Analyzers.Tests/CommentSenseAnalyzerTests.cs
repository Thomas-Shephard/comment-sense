using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CommentSenseAnalyzerTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task PublicClassWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMethodWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public void {|CSENSE001:MyMethod|}() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicConstructorWithoutDocumentationReportsFriendlyName()
    {
        const string testCode = """
            /// <summary>My class</summary>
            public class MyClass
            {
                public {|#0:MyClass|}(int x)
                {
                }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.MissingDocumentationRule)
            .WithLocation(0)
            .WithArguments("MyClass(int)");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task PublicParameterlessConstructorWithoutDocumentationReportsFriendlyName()
    {
        const string testCode = """
            /// <summary>My class</summary>
            public class MyClass
            {
                public {|#0:MyClass|}()
                {
                }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.MissingDocumentationRule)
            .WithLocation(0)
            .WithArguments("MyClass()");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task PublicFieldWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public int {|CSENSE001:MyField|};
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicPropertyWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public int {|CSENSE001:MyProperty|} { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicEventWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public event EventHandler {|CSENSE001:MyEvent|};
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ConstructorWithLowQualitySummaryReportsFriendlyName()
    {
        const string testCode = """
            /// <summary>My class</summary>
            public class MyClass
            {
                /// {|#0:<summary>MyClass(int)</summary>|}
                /// <param name="x">The value.</param>
                public MyClass(int x)
                {
                }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.LowQualityDocumentationRule)
            .WithLocation(0)
            .WithArguments("summary", "MyClass(int)");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task ConstructorWithStrayReturnsReportsFriendlyName()
    {
        const string testCode = """
            /// <summary>My class</summary>
            public class MyClass
            {
                /// <summary>This is the summary for the constructor.</summary>
                /// {|#0:<returns>Stray</returns>|}
                public MyClass()
                {
                }
            }
            """;

        var expected = new DiagnosticResult(CommentSenseRules.StrayReturnValueDocumentationRule)
            .WithLocation(0)
            .WithArguments("MyClass()");

        await VerifyCSenseAsync(testCode, expectedDiagnostics: [expected]);
    }

    [Test]
    public async Task PrivateOrInternalMemberWithoutDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            internal class MyClass
            {
                private void MyMethod() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DocumentedPublicClassWithValidSummaryDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DocumentedPublicPropertyWithValidSummaryAndValueDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the property.</value>
                public int MyProperty { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PublicEventWithInheritdocDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the base class.</summary>
            public class Base
            {
                /// <summary>This is a summary for the event.</summary>
                public virtual event EventHandler MyEvent;
            }

            /// <inheritdoc />
            public class Derived : Base
            {
                /// <inheritdoc />
                public override event EventHandler MyEvent;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FieldWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class C
            {
                /// <summary>This is a summary for the field.</summary>
                public int f;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task EventWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class C
            {
                /// <summary>This is a summary for the event.</summary>
                public event EventHandler E;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StaticConstructorWithoutDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                static MyClass() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DestructorWithoutDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                ~MyClass() { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PublicClassWithInvalidDocumentationTagsReportsDiagnostic()
    {
        const string testCode = """
            /// <para>This tag alone is not considered valid documentation by our rules</para>
            public class {|CSENSE001:MyClass|}
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ReportMissingInheritDocWhenImplicitDisabled()
    {
        const string testCode = """
            /// <summary>Base.</summary>
            public class Base {
                /// <summary>Documentation.</summary>
                public virtual void M() { }
            }

            /// <summary>Derived.</summary>
            public class Derived : Base {
                public override void {|CSENSE018:M|}() { }
            }
            """;

        var config = new Dictionary<string, string>
        {
            { "comment_sense.allow_implicit_inheritdoc", "false" },
            { "dotnet_diagnostic.CSENSE016.severity", "none" }
        };

        await VerifyCSenseAsync(testCode, configOptions: config);
    }
}
