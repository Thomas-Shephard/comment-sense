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
    public async Task InvalidExceptionCrefReportsDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:string|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefPointingToMethodReportsDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyMethod|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValidCustomExceptionCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                internal class MyException : Exception { }

                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="MyException">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SeeAlsoCrefToNonExceptionDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                /// <seealso cref="string"/>
                public class MyClass { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SeeAlsoCrefToExceptionDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                /// <seealso cref="Exception"/>
                public class MyClass { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionCrefPointingToAliasOfNonExceptionReportsDiagnostic()
    {
        const string testCode = """
            using System;
            using MyType = System.String;

            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyType|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefPointingToTypeParameterInClassDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                /// <typeparam name="T">Exception type</typeparam>
                public class MyClass<T> where T : Exception
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="T">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionCrefPointingToAliasDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            using MyEx = System.ArgumentException;

            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="MyEx">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionCrefPointingToAliasOfNamespaceReportsDiagnostic()
    {
        const string testCode = """
            using MyAlias = System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyAlias|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefWithAmbiguousErrorTypeDoesNotReportCsense014()
    {
        const string testCode = """
            using System;
            namespace N1 { /// <summary>Ex</summary> public class Ex<T> : Exception { } }
            namespace N2 { /// <summary>Ex</summary> public class Ex<T> : Exception { } }

            namespace MyNamespace
            {
                using N1;
                using N2;

                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE007:Ex{int}|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: true, compilerDiagnostics: Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None);
    }

    [Test]
    public async Task CrefInRemarksDoesNotReportInvalidException()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <remarks>See <see cref="string"/></remarks>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionCrefWithNoStandardLibraryDoesNotReportInvalidException()
    {
        const string testCode = """
            namespace System
            {
                /// <summary>Summary</summary>
                public class Object { }
                /// <summary>Summary</summary>
                public class Exception { }
            }
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="System.Exception">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
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

    [Test]
    public async Task ExceptionCrefPointingToNamespaceReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyNamespace|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefPointingToPropertyReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    public int MyProperty { get; set; }
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyProperty|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefPointingToFieldReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    public int MyField;
                    /// <summary>Summary</summary>
                    /// <exception cref="{|CSENSE014:MyField|}">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCrefPointingToTypeAliasInsideNamespaceDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                using MyEx = System.Exception;
                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="MyEx">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionCrefPointingToNestedTypeAliasDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            namespace MyNamespace
            {
                using MyExAlias = Container.MyEx;

                /// <summary>Summary</summary>
                public class Container 
                { 
                    /// <summary>Summary</summary>
                    public class MyEx : Exception {} 
                }

                /// <summary>Summary</summary>
                public class MyClass
                {
                    /// <summary>Summary</summary>
                    /// <exception cref="MyExAlias">Thrown when...</exception>
                    public void MyMethod() { }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
