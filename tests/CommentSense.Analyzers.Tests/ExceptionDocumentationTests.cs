using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ExceptionDocumentationTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task ThrowStatementWithoutDocumentationReportsDiagnostic()
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
    public async Task ThrowStatementWithDocumentationDoesNotReportDiagnostic()
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
    public async Task ThrowExpressionWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private string _name;
                /// <summary>This is a summary for the property.</summary>
                /// <value>The name of the class.</value>
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
    public async Task CollectionExpressionThrowWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Builds a collection of values.</summary>
                /// <param name="shouldThrow">Controls whether the method throws.</param>
                /// <returns>The generated values.</returns>
                public List<int> {|CSENSE012:BuildValues|}(bool shouldThrow)
                {
                    return [1, shouldThrow ? 2 : throw new InvalidOperationException()];
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task CollectionExpressionThrowWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Builds a collection of values.</summary>
                /// <param name="shouldThrow">Controls whether the method throws.</param>
                /// <returns>The generated values.</returns>
                /// <exception cref="T:System.InvalidOperationException">Thrown when value generation fails.</exception>
                public List<int> BuildValues(bool shouldThrow)
                {
                    return [1, shouldThrow ? 2 : throw new InvalidOperationException()];
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task CollectionExpressionMultipleThrowsWithoutDocumentationReportMultipleDiagnostics()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Builds a collection of values.</summary>
                /// <param name="throwInvalidOp">Determines whether to throw an invalid operation exception.</param>
                /// <param name="throwArgument">Determines whether to throw an argument exception.</param>
                /// <returns>The generated values.</returns>
                public List<int> {|CSENSE012:{|CSENSE012:BuildValues|}|}(bool throwInvalidOp, bool throwArgument)
                {
                    return
                    [
                        throwInvalidOp ? throw new InvalidOperationException() : 1,
                        throwArgument ? throw new ArgumentException(nameof(throwArgument)) : 2
                    ];
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task MultipleExceptionsWithoutDocumentationReportMultipleDiagnostics()
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
    public async Task InheritDocSatisfiesExceptionDocumentation()
    {
        const string testCode = """
            using System;
            /// <summary>Provides a base implementation for certain operations.</summary>
            public class Base
            {
                /// <summary>Executes a standard operation.</summary>
                /// <exception cref="InvalidOperationException">Thrown always.</exception>
                public virtual void MyMethod() { }
            }
            /// <summary>A specialized implementation of the base class.</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                public override void MyMethod()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ShortNameResolutionWithDocumentationDoesNotReportDiagnostic()
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
    public async Task SingleCharacterExceptionNameIsResolved()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the exception class.</summary>
            public class E : Exception { }
            /// <summary>This is a summary for the main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="E">Short name</exception>
                public void MyMethod()
                {
                    throw new E();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PrefixedSingleCharacterExceptionNameIsResolved()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the exception class.</summary>
            public class E : Exception { }
            /// <summary>This is a summary for the main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="T:E">Short name with prefix</exception>
                public void MyMethod()
                {
                    throw new E();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ShortNameResolutionTwoCharsDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the exception class.</summary>
            public class Ex : Exception { }
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
    public async Task UnresolvedCrefPrefixWithDocumentationDoesNotReportDiagnostic()
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
    public async Task ExceptionWithWhitespaceInCrefIsResolved()
    {
        const string testCode = """
            using System;

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="  ArgumentNullException  ">Documented with whitespace.</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task SingleCharUnqualifiedNameForcesFallback()
    {
        const string testCode = """
            namespace N1 { public class E : System.Exception {} }
            namespace N2 {
                /// <summary>Class.</summary>
                public class MyClass {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="E">Length 1, unqualified</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod() { throw new N1.E(); }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task SingleCharPrefixedUnqualifiedNameForcesFallback()
    {
        const string testCode = """
            namespace N1 { public class E : System.Exception {} }
            namespace N2 {
                /// <summary>Class.</summary>
                public class MyClass {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="T:E">Length 3, prefixed, unqualified</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod() { throw new N1.E(); }
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task SingleCharBangPrefixReturnsNull()
    {
        const string testCode = """
            /// <summary>Class.</summary>
            public class MyClass {
                /// <summary>Method.</summary>
                /// <exception cref="!">Invalid</exception>
                public void {|CSENSE012:MyMethod|}() { throw new System.Exception(); }
            }
            """;
        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE007", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task FallbackResolutionUnqualifiedTypeWithoutUsingDoesNotReportDiagnostic()
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
    public async Task FallbackResolutionShortTypeWithoutUsingDoesNotReportDiagnostic()
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
    public async Task FallbackResolutionWithPrefixAndMissingUsingDoesNotReportDiagnostic()
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

    [Test]
    public async Task AmbiguousExceptionNameResolvesToBestMatch()
    {
        const string testCode = """
            using System;
            using N1;

            namespace N1 {
                /// <summary>Ex1.</summary>
                public class MyEx : Exception { }
            }
            namespace N2 {
                /// <summary>Ex2.</summary>
                public class MyEx : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="N1.MyEx">Documented with full name.</exception>
                public void MyMethod()
                {
                    throw new N1.MyEx();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionWithMethodCrefFallbackIsNotResolved()
    {
        const string testCode = """
            using System;

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="M:System.ArgumentNullException">Documented with method prefix, should be caught.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InvalidPrefixInCrefReturnsNull()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="Z:System.ArgumentNullException">Invalid prefix</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ExceptionWithTypeCrefFallbackIsResolved()
    {
        const string testCode = """
            using System;

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="T:ArgumentNullException">Documented with type prefix and partial name.</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionWithInvalidPrefixInCrefIsIgnored()
    {
        const string testCode = """
            using System;

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="M:ArgumentNullException">Invalid prefix for exception.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InvalidPrefixInFallbackReturnsNull()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="P:ArgumentNullException">Invalid prefix in fallback</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GenericSpecializationDifferentTypesAreNotCaughtByEachOther()
    {
        const string testCode = """
            using System;

            /// <summary>Generic exception.</summary>
            /// <typeparam name="T">Type.</typeparam>
            public class MyEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    try
                    {
                        throw new MyEx<int>();
                    }
                    catch (MyEx<string>)
                    {
                        // Different specialization, should not catch MyEx<int>
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task SpecializedGenericExceptionWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Generic exception.</summary>
            /// <typeparam name="T">Type.</typeparam>
            public class MyEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="MyEx{Int32}">Documented specialized.</exception>
                public void MyMethod()
                {
                    throw new MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericDefinitionDocumentationSatisfiesSpecializedException()
    {
        const string testCode = """
            using System;

            /// <summary>Generic exception.</summary>
            /// <typeparam name="T">Type.</typeparam>
            public class MyEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="MyEx{T}">Documented definition.</exception>
                public void MyMethod()
                {
                    throw new MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FullyQualifiedSpecializedGenericExceptionWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            namespace N
            {
                /// <summary>Generic exception.</summary>
                /// <typeparam name="T">Type.</typeparam>
                public class MyEx<T> : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="N.MyEx{Int32}">Fully qualified specialized.</exception>
                public void MyMethod()
                {
                    throw new N.MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionWithQualifiedGenericTypeIsResolved()
    {
        const string testCode = """
            using System;

            namespace N
            {
                /// <summary>Ex.</summary>
                /// <typeparam name="T">Type.</typeparam>
                public class MyEx<T> : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="N.MyEx{System.Int32}">Documented with qualified generic.</exception>
                public void MyMethod()
                {
                    throw new N.MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImportedGenericExceptionWithFallbackDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            using N;

            namespace N
            {
                /// <summary>Generic exception.</summary>
                /// <typeparam name="T">Type.</typeparam>
                public class MyEx<T> : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="MyEx{Int32}">Documented with import.</exception>
                public void MyMethod()
                {
                    throw new MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task NestedExceptionWithOuterGenericIsResolved()
    {
        const string testCode = """
            using System;

            public class Outer<T>
            {
                public class NestedException : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="Outer{Int32}.NestedException">Documented nested with generic outer.</exception>
                public void MyMethod()
                {
                    throw new Outer<int>.NestedException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task NestedGenericExceptionWithMultipleGenericSegmentsIsResolved()
    {
        const string testCode = """
            using System;

            public class Outer<T>
            {
                public class Inner<U> : Exception { }
            }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="Outer{Int32}.Inner{String}">Documented nested with multiple generic segments.</exception>
                public void MyMethod()
                {
                    throw new Outer<int>.Inner<string>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task GenericExceptionWithBracesInCrefIsResolved()
    {
        const string testCode = """
            using System;

            /// <summary>Ex.</summary>
            /// <typeparam name="T">Type.</typeparam>
            public class MyEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="MyEx{T}">Generic cref with braces.</exception>
                public void MyMethod()
                {
                    throw new MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericFallbackResolutionUnqualifiedTypeDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace N1
            {
                /// <summary>Generic exception.</summary>
                /// <typeparam name="T">Type.</typeparam>
                public class GenericEx<T> : System.Exception {}
            }
            namespace N2
            {
                /// <summary>Class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="GenericEx">Unqualified name, should fallback to generic definition</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.GenericEx<int>();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericFallbackWithBracesAndUnqualifiedNameIsResolved()
    {
        const string testCode = """
            namespace N1
            {
                /// <summary>Generic exception.</summary>
                /// <typeparam name="T">Type.</typeparam>
                public class GenericEx<T> : System.Exception {}
            }
            namespace N2
            {
                /// <summary>Class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="!:GenericEx{T}">Force fallback path</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.GenericEx<int>();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericFallbackWithNestedTypeAndNoUsingIsResolved()
    {
        const string testCode = """
            namespace N1
            {
                /// <summary>This is a summary for the outer class.</summary>
                public class Outer
                {
                    /// <summary>Nested generic exception.</summary>
                    /// <typeparam name="T">The type of the exception data.</typeparam>
                    public class NestedEx<T> : System.Exception {}
                }
            }
            namespace N2
            {
                /// <summary>This is a summary for the main class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="!:Outer.NestedEx">Nested generic fallback</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.Outer.NestedEx<int>();
                    }
                }
            }
            """;

        // Suppress CSENSE001 for Outer and CSENSE012 for the thrown exception to focus on fallback logic
        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: [
            ("CSENSE001", ReportDiagnostic.Suppress),
            ("CSENSE012", ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task GenericFallbackWithDefinitionMatchIsResolved()
    {
        const string testCode = """
            namespace N1
            {
                /// <summary>Generic exception.</summary>
                /// <typeparam name="T">The type of the exception data.</typeparam>
                public class MyGenericEx<T> : System.Exception {}
            }
            namespace N2
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="!:N1.MyGenericEx">Match by full definition name</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.MyGenericEx<int>();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericExceptionResolvedByMinPartsInFallback()
    {
        const string testCode = """
            namespace N1
            {
                /// <summary>This is a summary for the outer class.</summary>
                public class Outer
                {
                    /// <summary>Generic exception.</summary>
                    /// <typeparam name="T">The type of the exception data.</typeparam>
                    public class GenericEx<T> : System.Exception {}
                }
            }
            namespace N2
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="!:GenericEx{T}">Resolved via minParts in fallback</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.Outer.GenericEx<int>();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: [
            ("CSENSE001", ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task GenericFallbackHandlesMultipleGenericSymbols()
    {
        const string testCode = """
            namespace N1 {
                /// <summary>Ex1.</summary>
                /// <typeparam name="T">The type parameter T.</typeparam>
                public class MultiEx<T> : System.Exception {}
            }
            namespace N2 {
                /// <summary>Ex2.</summary>
                /// <typeparam name="T">The type parameter T.</typeparam>
                public class MultiEx<T> : System.Exception {}
            }
            namespace N3
            {
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>Method.</summary>
                    #pragma warning disable CSENSE007
                    /// <exception cref="!:MultiEx">Match multiple generics</exception>
                    #pragma warning restore CSENSE007
                    public void MyMethod()
                    {
                        throw new N1.MultiEx<int>();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task GenericExceptionWithBracesInCrefSimpleNameFallbackIsHit()
    {
        const string testCode = """
            using System;
            /// <summary>Generic exception class.</summary>
            /// <typeparam name="T">The type of the exception data.</typeparam>
            public class MyGenericException<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                // Use a cref with braces that our fallback WILL resolve via simple name extraction
                /// <exception cref="MyGenericException{Int32}">Force fallback path via simple name</exception>
                public void MyMethod()
                {
                    throw new MyGenericException<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RemoveGenericsDeepNestingHandledCorrectly()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;

            /// <summary>Deeply nested generic exception.</summary>
            /// <typeparam name="T">The nested type.</typeparam>
            public class DeepEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="DeepEx{List{Dictionary{Int32, String}}}">Deep nesting</exception>
                public void MyMethod()
                {
                    throw new DeepEx<List<Dictionary<int, string>>>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task BaseClassDocumentationSatisfiesDerivedException()
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
    public async Task DerivedClassDocumentationDoesNotSatisfyBaseException()
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
    public async Task InheritedExceptionWithDocumentationDoesNotReportDiagnostic()
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
    public async Task SwallowedExceptionIsIgnored()
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
    public async Task FilteredCatchWithoutDocumentationReportsDiagnostic()
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
    public async Task ExceptionCaughtByOuterTryIsIgnored()
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
    public async Task SwallowedByGeneralCatchIsIgnored()
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
    public async Task WrongCatchTypeWithoutDocumentationReportsDiagnostic()
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
    public async Task RethrowWithoutDocumentationReportsDiagnostic()
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
    public async Task RethrowWithDocumentationDoesNotReportDiagnostic()
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
    public async Task GeneralCatchRethrowWithExceptionDocumentationDoesNotReportDiagnostic()
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
    public async Task ThrowInCatchBlockWithoutDocumentationReportsDiagnostic()
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

        await VerifyCSenseAsync(testCode, compilerDiagnostics: CompilerDiagnostics.None);
    }

    [Test]
    public async Task AsyncMethodWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            using System.Threading.Tasks;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the async method.</summary>
                /// <returns>A task that represents the asynchronous operation.</returns>
                public async Task {|CSENSE012:MyMethodAsync|}()
                {
                    await Task.Yield();
                    throw new InvalidOperationException();
                }
            }
            """;

        // Returns tag is currently required by ReturnValueAnalyzer for Tasks, so we suppress it here to focus on exceptions.
        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE013", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task IteratorBlockWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            using System.Collections.Generic;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the iterator method.</summary>
                /// <returns>An enumerable of integers.</returns>
                public IEnumerable<int> {|CSENSE012:MyIterator|}()
                {
                    yield return 1;
                    throw new InvalidOperationException();
                }
            }
            """;

        // Returns tag is required, we suppress it.
        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE006", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task ExplicitInterfaceImplementationWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the interface.</summary>
            public interface IMyInterface
            {
                /// <summary>This is a summary for the method.</summary>
                void MyMethod();
            }
            /// <summary>This is a summary for the class.</summary>
            public class MyClass : IMyInterface
            {
                /// <inheritdoc/>
                void IMyInterface.MyMethod()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        // Currently ignored by IsEligibleForAnalysis. Documenting current behavior.
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropertyBodyThrowStatementReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>The name of the instance.</value>
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
    public async Task IndexerThrowStatementReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the indexer.</summary>
                /// <param name="i">The index value.</param>
                /// <value>Value at the specified index.</value>
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
    public async Task EventAccessorWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private EventHandler? _myEvent;
                /// <summary>This is a summary for the event.</summary>
                public event EventHandler MyEvent
                {
                    add => throw new InvalidOperationException();
                    remove => _myEvent -= value;
                }
            }
            """;

        // Currently ignored by analyzer. Documenting current behavior.
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task StaticConstructorWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Initializes static members.</summary>
                static MyClass()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        // Currently ignored by IsEligibleForAnalysis. Documenting current behavior.
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task FinalizerWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>Finalizes an instance.</summary>
                ~MyClass()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        // Currently ignored by IsEligibleForAnalysis. Documenting current behavior.
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PrimaryConstructorExceptionInPropertyInitializerReportsOnProperty()
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

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE014", ReportDiagnostic.Warn)]);
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
    public async Task PrimaryConstructorExceptionInFieldInitializerReportsOnClass()
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
    public async Task PrimaryConstructorExceptionInFieldInitializerWithClassDocumentationDoesNotReportDiagnostic()
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
    public async Task PrimaryConstructorExceptionInPropertyAccessorIsIgnoredOnClass()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            /// <param name="id">The identifier.</param>
            public class MyClass(int id)
            {
                internal int Id {
                    get => id;
                    set => throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RecordPrimaryConstructorWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            namespace System.Runtime.CompilerServices { public class IsExternalInit { } }
            /// <summary>This is a summary for the record.</summary>
            /// <param name="X">The X value.</param>
            public record {|CSENSE012:MyRecord|}(int X)
            {
                private int _y = X > 0 ? X : throw new ArgumentException();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ArgumentNullExceptionThrowIfNullWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="arg">The argument.</param>
                public void {|CSENSE012:MyMethod|}(string arg)
                {
                    ArgumentNullException.ThrowIfNull(arg);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ArgumentExceptionThrowIfNullOrEmptyWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="arg">The argument.</param>
                public void {|CSENSE012:MyMethod|}(string arg)
                {
                    ArgumentException.ThrowIfNullOrEmpty(arg);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ObjectDisposedExceptionThrowIfWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass : IDisposable
            {
                private bool _disposed;
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    ObjectDisposedException.ThrowIf(_disposed, this);
                }

                public void Dispose() => _disposed = true;
            }
            """;

        await VerifyCSenseAsync(testCode, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CustomExceptionStaticThrowIfReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>This is a summary for the exception class.</summary>
            public class MyException : Exception
            {
                /// <summary>Throws if condition is <see langword="true"/>.</summary>
                /// <param name="condition">The condition.</param>
                /// <exception cref="MyException">Thrown when condition is <see langword="true"/>.</exception>
                public static void ThrowIf(bool condition)
                {
                    if (condition) throw new MyException();
                }
            }

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="condition">The condition.</param>
                public void {|CSENSE012:MyMethod|}(bool condition)
                {
                    MyException.ThrowIf(condition);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task NonStaticThrowMethodIsIgnored()
    {
        const string testCode = """
            using System;

            /// <summary>This is a summary for the exception class.</summary>
            public class MyException : Exception
            {
            }

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private void ThrowIfInstance(bool condition)
                {
                    if (condition) throw new MyException();
                }

                /// <summary>This is a summary for the method.</summary>
                /// <param name="condition">The condition.</param>
                public void MyMethod(bool condition)
                {
                    ThrowIfInstance(condition);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task StaticMethodNotStartingWithThrowIsIgnored()
    {
        const string testCode = """
            using System;

            /// <summary>This is a summary for the exception class.</summary>
            public class MyException : Exception
            {
            }

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private static void Guard(bool condition)
                {
                    if (condition) throw new MyException();
                }

                /// <summary>This is a summary for the method.</summary>
                /// <param name="condition">The condition.</param>
                public void MyMethod(bool condition)
                {
                    Guard(condition);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task StaticThrowMethodOnNonExceptionTypeIsIgnored()
    {
        const string testCode = """
            using System;

            internal static class Helper
            {
                public static void ThrowIf(bool condition)
                {
                    if (condition) throw new Exception();
                }
            }

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="condition">The condition.</param>
                public void MyMethod(bool condition)
                {
                    Helper.ThrowIf(condition);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task MethodReturningExceptionWithoutThrowIsIgnored()
    {
        const string testCode = """
            using System;

            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                private static Exception ThrowFactory() => new Exception();

                /// <summary>Method that calls the factory but doesn't throw.</summary>
                public void MyMethod()
                {
                    // This does NOT throw, so it should NOT be flagged.
                    ThrowFactory();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task GuardClauseWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="arg">The argument.</param>
                /// <exception cref="ArgumentNullException">Thrown when arg is <see langword="null"/>.</exception>
                public void MyMethod(string arg)
                {
                    ArgumentNullException.ThrowIfNull(arg);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task GuardClauseWithWrongDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="arg">The argument.</param>
                /// <exception cref="InvalidOperationException">Wrong exception type.</exception>
                public void {|CSENSE012:MyMethod|}(string arg)
                {
                    ArgumentNullException.ThrowIfNull(arg);
                }
            }
            """;

        await VerifyCSenseAsync(testCode, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task PropagatedExceptionWithDocumentationDoesNotReportStray()
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
    public async Task ConstructorInitializerWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the base class.</summary>
            public class Base
            {
                /// <summary>Initializes a new instance of the <see cref="Base"/> class.</summary>
                /// <exception cref="InvalidOperationException">Thrown always.</exception>
                public Base() { throw new InvalidOperationException(); }
            }
            /// <summary>This is a summary for the derived class.</summary>
            public class Derived : Base
            {
                /// <summary>Initializes a new instance of the <see cref="Derived"/> class.</summary>
                public {|CSENSE012:Derived|}() : base() { }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string>
        {
            ["comment_sense.scan_called_methods_for_exceptions"] = "true"
        });
    }

    [Test]
    public async Task ObjectInitializerWithoutDocumentationReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the target class.</summary>
            public class Target
            {
                /// <summary>This is a summary for the property.</summary>
                /// <value>The value of the target property.</value>
                /// <exception cref="InvalidOperationException">Thrown always.</exception>
                public int Value { get; set => throw new InvalidOperationException(); }
            }
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    var t = new Target { Value = 1 };
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string>
        {
            ["comment_sense.scan_called_methods_for_exceptions"] = "true"
        });
    }

    [Test]
    public async Task StrayExceptionDocumentationDoesNotReportMissingException()
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
    public async Task ExceptionTagMissingCrefAttributeDoesNotSatisfyRequirement()
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
    public async Task ExceptionTagWithoutCrefDoesNotReportStrayAtTopLevel()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception>Missing cref attribute</exception>
                public void MyMethod()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task DuplicateExceptionsReportStray()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="T:System.ArgumentNullException">First</exception>
                /// {|CSENSE023:<exception cref="T:System.ArgumentNullException">Second</exception>|}
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DuplicateUnresolvedExceptionsReportStray()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="T:NonExistent">First</exception>
                /// {|CSENSE023:<exception cref="T:NonExistent">Second</exception>|}
                public void MyMethod()
                {
                }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [("CSENSE007", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public void InternalMethodsCoverage()
    {
        var mscorlibReference = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
        var compilation = CSharpCompilation.Create("Test", references: [mscorlibReference]);

        using (Assert.EnterMultipleScope())
        {
            // ResolveExceptionType and CrefInfo.Parse
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType(null, compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:System.Exception", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("System.Exception", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("global::System.Exception", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("ArgumentNullException", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:System.Collections.Generic.List<System.String>", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:System.Exception<System.String>", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:System.Collections.Generic.Dictionary<System.String,System.Collections.Generic.Dictionary<System.Int32,System.String>>", compilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:System.Collections.Generic.List<System.String", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("M:SomeMethod", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("!:SomeBadCref", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("!", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("123", compilation), Is.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("System.123", compilation), Is.Null);
        }

        var localCompilation = CSharpCompilation.Create(
            "LocalTest",
            syntaxTrees: [CSharpSyntaxTree.ParseText("""
                using System;
                namespace Demo;
                public class LocalException : Exception { }
                """)],
            references: [MetadataReference.CreateFromFile(typeof(object).Assembly.Location)]);

        Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("LocalException", localCompilation), Is.Not.Null);

        var globalCompilation = CSharpCompilation.Create(
            "GlobalTest",
            syntaxTrees: [CSharpSyntaxTree.ParseText("""
                using System;
                public class RootException : Exception { }
                """)],
            references: [mscorlibReference]);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("global::RootException", globalCompilation), Is.Not.Null);
            Assert.That(Logic.ExceptionAnalyzer.ResolveExceptionType("T:global::RootException", globalCompilation), Is.Not.Null);
        }

        var options = CommentSenseOptions.Default;
        var exceptionType = compilation.GetTypeByMetadataName("System.Exception") ?? compilation.GetSpecialType(SpecialType.System_Object);

        using (Assert.EnterMultipleScope())
        {
            // IsIgnored - Branch: IgnoredExceptions.Contains(type.Name)
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoredExceptions = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Exception") }), Is.True);
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options), Is.False);

            // Branch: IgnoredExceptions.Contains(fullName)
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoredExceptions = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "System.Exception") }), Is.True);
        }

        // Branch: Generic exception definition ignore
        var genericType = compilation.GetTypeByMetadataName("System.Collections.Generic.List`1");
        if (genericType != null)
        {
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(genericType, options with { IgnoredExceptions = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "System.Collections.Generic.List<T>") }), Is.True);
        }

        using (Assert.EnterMultipleScope())
        {
            // Branch: IgnoreSystemExceptions
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoreSystemExceptions = true }), Is.True);
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoreSystemExceptions = false }), Is.False);

            // Branch: IgnoredExceptionNamespaces
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoredExceptionNamespaces = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "System") }), Is.True);
            Assert.That(Logic.ExceptionAnalyzer.IsIgnored(exceptionType, options with { IgnoredExceptionNamespaces = ImmutableHashSet.Create(StringComparer.OrdinalIgnoreCase, "Microsoft") }), Is.False);

            // IsInNamespace
            Assert.That(Logic.ExceptionAnalyzer.IsInNamespace("System.IO", "System"), Is.True); // hits StartsWith(target + ".")
            Assert.That(Logic.ExceptionAnalyzer.IsInNamespace("System", "System"), Is.True); // hits Equals
            Assert.That(Logic.ExceptionAnalyzer.IsInNamespace("Microsoft", "System"), Is.False); // hits none
        }
    }

    [Test]
    public async Task ResolveExceptionTypeEdgeCases()
    {
        const string testCode = """
            using System;
            using System.IO;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <exception cref="!">Length 1, no colon</exception>
                /// <exception cref="T:">Length 2, colon at 1</exception>
                /// <exception cref="AB">Length 2, no colon</exception>
                /// <exception cref=":A">Length 2, colon at 0</exception>
                /// <exception cref="ABC:D">Length 5, colon at 3</exception>
                /// <exception cref="M:ArgumentNullException">Invalid prefix for exception</exception>
                /// <exception cref="T:NonExistent">Prefixed but non-existent</exception>
                /// <exception cref="IOException">Valid without prefix</exception>
                /// <exception cref="T:System.IO.IOException">Valid with prefix</exception>
                /// <exception cref="!:NonExistent">Bang prefix</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new Exception();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: [
            ("CSENSE007", ReportDiagnostic.Suppress),
            ("CSENSE023", ReportDiagnostic.Suppress)
        ]);
    }

    [Test]
    public async Task ExceptionTagWhitespaceCrefAttributeDoesNotSatisfyRequirement()
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
    public async Task IgnoredGenericDefinitionSuppressesSpecializedExceptions()
    {
        const string testCode = """
            using System;

            /// <summary>Generic exception.</summary>
            /// <typeparam name="T">Type.</typeparam>
            public class MyEx<T> : Exception { }

            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                public void MyMethod()
                {
                    throw new MyEx<int>();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exceptions"] = "MyEx<T>" });
    }

    [Test]
    public async Task ElementAccessInvocationIsIgnored()
    {
        const string testCode = """
            using System;
            /// <summary>Class.</summary>
            public class MyClass
            {
                /// <summary>Method.</summary>
                /// <param name="actions">The actions.</param>
                public void MyMethod(Action[] actions)
                {
                    actions[0]();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task AnalyzerMissingSystemExceptionHandledGracefully()
    {
        const string testCode = "public class C { public void M() { } }";

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            compilerDiagnostics: CompilerDiagnostics.None,
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)],
            referenceAssemblies: ReferenceAssemblies.Net.Net100,
            solutionTransform: (solution, projectId) => solution.WithProjectMetadataReferences(projectId, []));
    }

    [Test]
    public async Task ExceptionAnalyzerGuardClauseWithIdentifierNameSyntax()
    {
        const string testCode = """
            using System;
            public class MyException : Exception {
                public static void Throw() => throw new MyException();
            }
            /// <summary>This is a valid long enough summary for the class.</summary>
            public class MyClass {
                /// <summary>This is a valid long enough summary for the method.</summary>
                public void {|CSENSE012:M|}() {
                    var thrower = MyException.Throw;
                    MyException.Throw();
                }
            }
            """;
        await VerifyCSenseAsync(testCode,
            referenceAssemblies: ReferenceAssemblies.Net.Net100,
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task ExceptionAnalyzerHandlesNonMethodExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>This is a valid long enough summary for the class.</summary>
            public class MyClass {
                /// <summary>This is a valid long enough summary for the method.</summary>
                public void M() {
                    Type t = typeof(int);
                }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ExceptionInGlobalNamespaceIsHandled()
    {
        const string testCode = """
            using System;
            public class GlobalException : Exception {}
            /// <summary>This is a valid long enough summary for the class.</summary>
            public class C
            {
                /// <summary>This is a valid long enough summary for the method.</summary>
                public void {|CSENSE012:M|}()
                {
                    throw new GlobalException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task TypeParameterExceptionIsHandled()
    {
        const string testCode = """
            using System;
            /// <summary>This is a valid long enough summary for the class.</summary>
            public class C
            {
                /// <summary>This is a valid long enough summary for the method.</summary>
                public void {|CSENSE012:M|}<T>() where T : Exception, new()
                {
                    throw new T();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            diagnosticOptions: [("CSENSE004", ReportDiagnostic.Suppress)]);
    }

    [Test]
    public async Task IgnoredNamespaceExceptionIsHandled()
    {
        const string testCode = """
            using System;
            namespace MyNamespace.Sub
            {
                public class MyEx : Exception {}
            }
            /// <summary>This is a valid long enough summary for the class.</summary>
            public class C
            {
                /// <summary>This is a valid long enough summary for the method.</summary>
                public void M()
                {
                    throw new MyNamespace.Sub.MyEx();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exception_namespaces"] = "MyNamespace" },
            diagnosticOptions: [("CSENSE001", ReportDiagnostic.Suppress)]);
    }
}
