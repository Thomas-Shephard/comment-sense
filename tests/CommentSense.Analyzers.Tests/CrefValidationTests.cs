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
                /// Summary
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
                    /// <summary>Summary</summary>
                    public void MyMethod() { }
                    /// <summary>Summary</summary>
                    public int MyProperty { get; set; }
                    /// <summary>Summary</summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE007:Unresolved|}"/>
                    /// </summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE007:UnresolvedException|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InternalMemberWithUnresolvedCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
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
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>
                    /// See <see cref="M"/>
                    /// </summary>
                    public void M() { }
                    /// <summary>Summary</summary>
                    /// <param name="i"><see cref="int"/></param>
                    public void M(int i) { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task CrefOnNamespaceReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>
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
            /// See <see cref="Unresolved"/>
            /// </summary>

            using System;
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
