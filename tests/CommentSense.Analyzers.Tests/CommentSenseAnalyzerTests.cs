using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CommentSenseAnalyzerTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task PublicClassWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            public class [|MyClass|]
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
                public void [|MyMethod|]() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrivateClassWithoutDocumentationDoesNotReportDiagnostic()
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
    public async Task DocumentedPublicClassDoesNotReportDiagnostic()
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
    public async Task PublicFieldWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                public int [|MyField|];
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
                public int [|MyProperty|] { get; set; }
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
                public event EventHandler [|MyEvent|];
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedPublicPropertyDoesNotReportDiagnostic()
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
    public async Task PublicClassWithInvalidDocumentationTagsReportsDiagnostic()
    {
        const string testCode = """
            /// <para>This tag alone is not considered valid documentation by our rules</para>
            public class [|MyClass|]
            {
            }
            """;

        await VerifyCSenseAsync(testCode);
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
    public async Task StaticConstructorDoesNotReportDiagnostic()
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
    public async Task DestructorDoesNotReportDiagnostic()
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
    public async Task PropertyWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a summary for the class.</summary>
            public class C
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the property.</value>
                public int P { get; set; }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
