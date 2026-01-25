using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ExceptionDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task MissingExceptionDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedExceptionDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.ArgumentNullException">Thrown when...</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DocumentingBaseClassSatisfiesDerivedException()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.ArgumentException">Thrown when...</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DocumentingDerivedClassDoesNotSatisfyBaseException()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.ArgumentNullException">Thrown when...</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task StrayExceptionDocumentationReportsNoDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.ArgumentNullException">Thrown when...</exception>
                public void MyMethod()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ThrowExpressionReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private string _name;
                /// <summary>This is a summary for the property.</summary>
                /// <value>The name.</value>
                public string {|CSENSE012:Name|}
                {
                    get => _name ?? throw new InvalidOperationException();
                    set => _name = value;
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionsInLambdasAreIgnored()
    {
        const string testCode = """
            using System;
            using System.Linq;
            using System.Collections.Generic;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    IEnumerable<int> x = new[] { 1 }.Select<int, int>(i => i > 0 ? i : throw new InvalidOperationException());
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionsInLocalFunctionsAreIgnored()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    void Local() => throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PrimaryConstructorExceptionsInPropertyInitializersAreReportedOnProperty()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            public class MyClass(int x)
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>The Y property value.</value>
                public int {|CSENSE012:Y|} { get; } = x > 0 ? x : throw new ArgumentException();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrimaryConstructorIgnoresExceptionsInMethods()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            public class MyClass(int x)
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.InvalidOperationException">Thrown when...</exception>
                public void SomeMethod()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropertyWithBodyThatThrowsReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>Value of the class.</value>
                public string {|CSENSE012:Name|}
                {
                    get
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task RethrowReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    try { }
                    catch (ArgumentException)
                    {
                        throw;
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task RethrowWithDocumentedCaughtExceptionDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.ArgumentException">Thrown when...</exception>
                public void MyMethod()
                {
                    try { }
                    catch (ArgumentException)
                    {
                        throw;
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GeneralCatchRethrowReportsSystemException()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.Exception">Thrown when...</exception>
                public void MyMethod()
                {
                    try { }
                    catch
                    {
                        throw;
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task NestedExceptionTagsAreIgnored()
    {
        const string testCode = """
            using System;
            /// <summary>
            /// This is a summary for the class.
            /// <exception cref="T:System.ArgumentNullException">This is nested and should be ignored</exception>
            /// </summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IndexerExceptionsAreReported()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="i">The index value.</param>
                /// <value>Value at the index.</value>
                public int {|CSENSE012:{|CSENSE012:this|}|}[int i]
                {
                    get => throw new IndexOutOfRangeException();
                    set => throw new ArgumentOutOfRangeException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrimaryConstructorIgnoresIndexerExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            public class MyClass(int x)
            {
                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="i">The index value.</param>
                /// <exception cref="T:System.IndexOutOfRangeException">Thrown when...</exception>
                public int {|CSENSE014:this|}[int i]
                {
                    get => throw new IndexOutOfRangeException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", Microsoft.CodeAnalysis.ReportDiagnostic.Warn)]);
    }

    [Test]
    public async Task PrimaryConstructorIgnoresSecondaryConstructorExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            public class MyClass(int x)
            {
                /// <summary>This is a summary for the constructor.</summary>
                public {|CSENSE012:MyClass|}() : this(0)
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionTagWithMissingCrefAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception>Missing cref attribute</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionTagWithWhitespaceCrefAttributeDoesNotCountAsDocumented()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE007:|}">Whitespace cref attribute</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentingInheritedExceptionSatisfiesCheck()
    {
        const string testCode = """
            using System;
            using System.IO;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:System.IO.IOException">Thrown when...</exception>
                public void MyMethod()
                {
                    throw new FileNotFoundException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task UnresolvedCrefPrefixesAreHandled()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="!:System.ArgumentNullException">Unresolved prefix</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ShortNameResolutionWorks()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="ArgumentNullException">Short name</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MultipleExceptionsReportMultipleDiagnostics()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="x">The input value.</param>
                public void {|CSENSE012:{|CSENSE012:MyMethod|}|}(int x)
                {
                    if (x < 0) throw new ArgumentOutOfRangeException();
                    if (x == 0) throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrimaryConstructorFieldInitializerExceptionsAreReportedOnClass()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            public class {|CSENSE012:MyClass|}(int x)
            {
                private int _y = x > 0 ? x : throw new ArgumentException();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DocumentedExceptionOnClassSatisfiesPrimaryConstructorInitializer()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="x">The x value.</param>
            /// <exception cref="T:System.ArgumentException">Thrown when...</exception>
            public class MyClass(int x)
            {
                private int _y = x > 0 ? x : throw new ArgumentException();
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropagatedExceptionDocumentationIsNotStray()
    {
        const string testCode = """
            using System;
            using System.IO;

            /// <summary>This is a summary for the callee class.</summary>
            public class Callee
            {
                /// <summary>This is a summary for the work method.</summary>
                /// <exception cref="T:System.IO.IOException">Thrown when...</exception>
                public void DoWork() { }
            }

            /// <summary>This is a summary for the caller class.</summary>
            public class Caller
            {
                /// <summary>This is a summary for the calling method.</summary>
                /// <param name="c">The callee instance.</param>
                /// <exception cref="T:System.IO.IOException">Propagated from Callee</exception>
                public void MyMethod(Callee c)
                {
                    c.DoWork();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SwallowedExceptionDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    try
                    {
                        throw new ArgumentNullException();
                    }
                    catch (ArgumentNullException)
                    {
                        // Swallowed
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FilteredCatchIsTreatedAsUncaughtAndReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    try
                    {
                        throw new ArgumentNullException();
                    }
                    catch (ArgumentNullException) when (false)
                    {
                        // Filtered, so it might escape
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ThrowInCatchBlockReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    try { }
                    catch (ArgumentException)
                    {
                        throw new InvalidOperationException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionCaughtByOuterTryDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    try
                    {
                        try
                        {
                            throw new ArgumentNullException();
                        }
                        finally { }
                    }
                    catch (ArgumentNullException)
                    {
                        // Handled by outer catch
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ShortNameResolutionWorksWithTwoChars()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the exception class.</summary>
            class Ex : Exception { }
            /// <summary>This is a summary for the main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="Ex">Short name</exception>
                public void MyMethod()
                {
                    throw new Ex();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SwallowedByGeneralCatchDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    try
                    {
                        throw new ArgumentNullException();
                    }
                    catch
                    {
                        // Swallowed by general catch
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task WrongCatchDoesNotSuppressDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    try
                    {
                        throw new ArgumentNullException();
                    }
                    catch (InvalidOperationException)
                    {
                        // Wrong catch
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task OrphanedRethrowReportsSystemException()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    // Orphaned throw (compiler error CS0156, ignored by test config)
                    throw;
                }
            }
            """;

        await VerifyCSenseAsync(testCode, compilerDiagnostics: Microsoft.CodeAnalysis.Testing.CompilerDiagnostics.None);
    }

    [Test]
    public async Task FallbackResolutionForUnqualifiedTypeWithoutUsing()
    {
        const string testCode2 = """
            namespace N1 { /// <summary>This is a summary for the exception class.</summary>
            public class MyEx : System.Exception {} }
            namespace N2
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    #pragma warning disable CSENSE007 // Unresolved cref (expected)
                    /// <exception cref="MyEx">Not imported, so standard resolution fails</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.MyEx();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode2, expectDiagnostic: false);
    }

    [Test]
    public async Task FallbackResolutionForShortTypeWithoutUsing()
    {
        const string testCode = """
            namespace N1 { /// <summary>This is a summary for the exception class.</summary>
            public class Ex : System.Exception {} }
            namespace N2
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    #pragma warning disable CSENSE007 // Unresolved cref (expected)
                    /// <exception cref="Ex">Not imported, short name</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.Ex();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FallbackResolutionWithPrefixAndMissingUsing()
    {
        const string testCode = """
            namespace N1 { /// <summary>This is a summary for the exception class.</summary>
            public class MyEx : System.Exception {} }
            namespace N2
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    #pragma warning disable CSENSE007 // Unresolved cref (expected)
                    /// <exception cref="T:MyEx">Prefix present, but not imported</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.MyEx();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
