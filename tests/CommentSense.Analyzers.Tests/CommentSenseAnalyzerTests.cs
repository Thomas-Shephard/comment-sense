using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CommentSenseAnalyzerTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    #region Missing Documentation (CSENSE001)

    [Test]
    public async Task PublicClass_WithoutDocumentation_ReportsDiagnostic()
    {
        const string testCode = """
            public class {|CSENSE001:MyClass|}
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMethod_WithoutDocumentation_ReportsDiagnostic()
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
    public async Task PublicConstructor_WithoutDocumentation_ReportsFriendlyName()
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
    public async Task PublicParameterlessConstructor_WithoutDocumentation_ReportsFriendlyName()
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
    public async Task PublicField_WithoutDocumentation_ReportsDiagnostic()
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
    public async Task PublicProperty_WithoutDocumentation_ReportsDiagnostic()
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
    public async Task PublicEvent_WithoutDocumentation_ReportsDiagnostic()
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

    #endregion

    #region Quality and Stray Documentation

    [Test]
    public async Task Constructor_WithLowQualitySummary_ReportsFriendlyName()
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
    public async Task Constructor_WithStrayReturns_ReportsFriendlyName()
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

    #endregion

    #region Exclusions (No Diagnostics)

    [Test]
    public async Task PrivateOrInternalMember_WithoutDocumentation_DoesNotReportDiagnostic()
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
    public async Task DocumentedPublicClass_WithValidSummary_DoesNotReportDiagnostic()
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
    public async Task DocumentedPublicProperty_WithValidSummaryAndValue_DoesNotReportDiagnostic()
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
    public async Task PublicEvent_WithInheritdoc_DoesNotReportDiagnostic()
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
    public async Task Field_WithDocumentation_DoesNotReportDiagnostic()
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
    public async Task Event_WithDocumentation_DoesNotReportDiagnostic()
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
    public async Task StaticConstructor_WithoutDocumentation_DoesNotReportDiagnostic()
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
    public async Task Destructor_WithoutDocumentation_DoesNotReportDiagnostic()
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

    #endregion

    #region Edge Cases

    [Test]
    public async Task PublicClass_WithInvalidDocumentationTags_ReportsDiagnostic()
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
    public async Task NestedParam_InsideSummary_DoesNotCountAsPrimaryParamDocumentation()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>
                /// <para>This is a nested <param name="p1">p1</param> tag.</para>
                /// </summary>
                /// {|CSENSE016:<param name="p1">p1</param>|}
                public void MyMethod(int p1) { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    #endregion
}
