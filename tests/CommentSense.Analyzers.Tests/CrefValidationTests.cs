using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CrefValidationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task UnresolvedSeeCrefReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>
                /// This is a summary for the class.
                /// See <see cref="{|CSENSE007:UnresolvedType|}"/>
                /// </summary>
                public class MyClass { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedSeeAlsoCrefReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>
                /// This is a summary for the class.
                /// </summary>
                /// <seealso cref="{|CSENSE007:UnresolvedType|}"/>
                public class MyClass { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ResolvedSeeCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>
                /// See <see cref="MyClass"/>
                /// See <see cref="M:MyNamespace.MyClass.MyMethod"/>
                /// See <see cref="P:MyNamespace.MyClass.MyProperty"/>
                /// See <see cref="F:MyNamespace.MyClass.MyField"/>
                /// See <see cref="T:System.String"/>
                /// See <see cref="MyMethod"/>
                /// See <see cref="MyProperty"/>
                /// See <see cref="MyField"/>
                /// </summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void MyMethod() { }
                    /// <summary>This is a summary for the property.</summary>
                    /// <value>Value of the property.</value>
                    public int MyProperty { get; set; }
                    /// <summary>This is a summary for the field.</summary>
                    public int MyField;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task UnresolvedCrefInMethodReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the method.
                    /// See <see cref="{|CSENSE007:Unresolved|}"/>
                    /// </summary>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedCrefInPropertyReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the property.
                    /// See <see cref="{|CSENSE007:Unresolved|}"/>
                    /// </summary>
                    /// <value>Value of the property.</value>
                    public int MyProperty { get; set; }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedCrefInFieldReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the field.
                    /// See <see cref="{|CSENSE007:Unresolved|}"/>
                    /// </summary>
                    public int MyField;
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedCrefInEventReportsDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the event.
                    /// See <see cref="{|CSENSE007:Unresolved|}"/>
                    /// </summary>
                    public event EventHandler MyEvent;
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task UnresolvedExceptionCrefReportsDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="{|CSENSE007:UnresolvedException|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [TestCase("Ordinary")]
    [TestCase("ArgumentException")]
    public async Task OutOfScopeCrefReportsUnresolvedReference(string name)
    {
        var source = $$"""
            namespace N { internal class Ordinary {} }
            /// <summary>Container.</summary>
            public class C
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="{|CSENSE007:{{name}}|}">Failure details.</exception>
                public void M() {}
            }
            """;
        await VerifyCSenseAsync(source);
    }

    [TestCase("C", false)]
    [TestCase("{|CSENSE007:Failure|}", true)]
    public async Task ExceptionCrefWithoutSystemExceptionIsHandled(string cref, bool expectDiagnostic)
    {
        var source = $$"""
            namespace N { internal class Failure {} }
            /// <summary>Container.</summary>
            public class C
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="{{cref}}">Failure details.</exception>
                public void M() {}
            }
            """;
        await VerifyCSenseAsync(source, expectDiagnostic: expectDiagnostic,
            compilerDiagnostics: Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None,
            solutionTransform: (solution, projectId) => solution.WithProjectMetadataReferences(projectId, []));
    }

    [Test]
    public async Task InternalMemberWithUnresolvedCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the method.
                    /// See <see cref="Unresolved"/>
                    /// </summary>
                    internal void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task AmbiguousCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>
                    /// This is a summary for the method.
                    /// See <see cref="M"/>
                    /// </summary>
                    public void M() { }
                    /// <summary>This is a summary for the other method.</summary>
                    /// <param name="i"><see cref="int"/></param>
                    public void M(int i) { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task AmbiguousExceptionCrefIsNotClassifiedAsInvalidType()
    {
        const string source = """
            /// <summary>Container.</summary>
            public class C
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="M">Failure details.</exception>
                public void M() {}
                /// <summary>Performs other work.</summary>
                /// <param name="value">Input data.</param>
                public void M(int value) {}
            }
            """;
        await VerifyCSenseAsync(source, expectDiagnostic: false);
    }

    [Test]
    public async Task CrefOnNamespaceReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>
            /// This is a summary for the namespace.
            /// See <see cref="{|CSENSE007:Unresolved|}"/>
            /// </summary>
            namespace MyNamespace { }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CrefOnCompilationUnitDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>
            /// This is a summary for the compilation unit.
            /// See <see cref="Unresolved"/>
            /// </summary>

            using System;
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FuzzyMatchWithMultipleExceptionsReportsSuggestion()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <exception cref="{|CSENSE007:ArgNull|}">Typo.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    if (true) throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NoFuzzyMatchWithMultipleExceptionsReturnsNull()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <exception cref="{|CSENSE007:TotallyUnrelated|}">No match.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    if (true) throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task NoFuzzyMatchWithMultipleExceptionsReturnsNullExplicit()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <exception cref="{|CSENSE007:A|}">No match, too short.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    if (true) throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SingleExceptionNoSimilarityProceedsToStep3AndReturnsNull()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="{|CSENSE007:X|}">No match, too short.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task FuzzyMatchWithHighSimilarityReturnsSuggestion()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <exception cref="{|CSENSE007:ArgumentNullEx|}">Typo with high similarity.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    if (true) throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task FuzzyMatchWithQualifiedNameReportsSuggestion()
    {
        const string testCode = """
            using System;

            /// <summary>This is a valid summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a valid summary for the method.</summary>
                /// <exception cref="{|CSENSE007:System.ArgNull|}">Qualified typo.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }
}
