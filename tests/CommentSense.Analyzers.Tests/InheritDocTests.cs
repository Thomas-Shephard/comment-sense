using CommentSense.TestHelpers;
using CommentSense.Core;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InheritDocTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    private static readonly (string Id, ReportDiagnostic Severity)[] SuppressMissingDocs =
    [
        (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress)
    ];

    private static readonly (string Id, ReportDiagnostic Severity)[] SuppressMissingDocsAndQuality =
    [
        (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
        (CommentSenseDiagnosticIds.LowQualityDocumentationId, ReportDiagnostic.Suppress)
    ];

    [Test]
    public async Task InvalidInheritDocReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a class.</summary>
            public class MyClass
            {
                /// <inheritdoc/>
                public void {|CSENSE026:MyMethod|}() { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ValidInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass
            {
                /// <summary>A virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>A derived class.</summary>
            public class MyClass : BaseClass
            {
                /// <inheritdoc/>
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithCrefDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a class.</summary>
            public class MyClass
            {
                /// <summary>This is the original method.</summary>
                public void M2() {}

                /// <inheritdoc cref="M2"/>
                public void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithUndocumentedOverrideTargetReportsDiagnostic()
    {
        const string testCode = """
            public class BaseClass
            {
                public virtual void M() {}
            }

            /// <summary>Derived.</summary>
            public class DerivedClass : BaseClass
            {
                /// <inheritdoc/>
                public override void {|CSENSE026:M|}() {}
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocAllowsGrandBaseDocumentationThroughOverrideChain()
    {
        const string testCode = """
            /// <summary>Base.</summary>
            public class BaseClass
            {
                /// <summary>Documented virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>Middle.</summary>
            public class MiddleClass : BaseClass
            {
                public override void M() {}
            }

            /// <summary>Derived.</summary>
            public class DerivedClass : MiddleClass
            {
                /// <inheritdoc/>
                public override void M() {}
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocWithUnresolvableCrefReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class MyClass
            {
                /// <inheritdoc cref="MissingMember"/>
                public void {|CSENSE026:M|}() {}
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            diagnosticOptions:
            [
                (CommentSenseDiagnosticIds.UnresolvedCrefId, ReportDiagnostic.Suppress)
            ]);
    }

    [Test]
    public async Task InheritDocWithUndocumentedCrefTargetReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class MyClass
            {
                void Undocumented() {}

                /// <inheritdoc cref="Undocumented"/>
                public void {|CSENSE026:M|}() {}
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            diagnosticOptions:
            [
                (CommentSenseDiagnosticIds.InaccessibleCrefId, ReportDiagnostic.Suppress)
            ]);
    }

    [Test]
    public async Task InheritDocWithDocumentedCrefTargetUsingElementSyntaxDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class MyClass
            {
                /// <summary>Documented member.</summary>
                public void M2() {}

                /// <inheritdoc cref="M2"></inheritdoc>
                public void M() {}
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocOnTypeWithUndocumentedTargetsReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase {}

            /// <inheritdoc/>
            public class {|CSENSE026:MyClass|} : IBase {}
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnFieldWithoutInheritanceTargetsReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Type.</summary>
            public class MyClass
            {
                /// <inheritdoc/>
                public int {|CSENSE026:Value|};
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocOnExplicitInterfaceImplementationWithDocumentedTargetDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Documented member.</summary>
                void M();
            }

            /// <summary>Type.</summary>
            public class MyClass : IBase
            {
                /// <inheritdoc/>
                void IBase.M() {}
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocOnImplicitInterfaceImplementationWithDocumentedTargetDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Documented member.</summary>
                void M();
            }

            /// <summary>Type.</summary>
            public class MyClass : IBase
            {
                /// <inheritdoc/>
                public void M() {}
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocOnExplicitMethodWithPrivateVisibilityDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Documented method.</summary>
                void M();
            }

            /// <summary>Type.</summary>
            public class MyClass : IBase
            {
                /// <inheritdoc/>
                void IBase.M() {}
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Private"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: options, diagnosticOptions: SuppressMissingDocsAndQuality);
    }

    [Test]
    public async Task InheritDocOnInterfaceFieldWithMethodNameReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                void F();
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                static int {|CSENSE026:F|} = 0;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfaceFieldWithBaseFieldReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                static int F = 0;
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                static int {|CSENSE026:F|} = 1;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfaceMethodWithStaticMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                void M();
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                static void {|CSENSE026:M|}() {}
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfaceMethodWithRefReturnMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                int M();
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                new ref int {|CSENSE026:M|}();
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfaceMethodWithTypeParameterCountMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                void M<T>();
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                new void {|CSENSE026:M|}();
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfacePropertyWithStaticMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                int P { get; }
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                static int {|CSENSE026:P|} => 0;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfacePropertyWithRefReturnMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                int P { get; }
            }

            public interface IDerived : IBase
            {
                private static int _value;
                /// <inheritdoc/>
                new ref int {|CSENSE026:P|} => ref _value;
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task InheritDocOnInterfaceIndexerWithParameterCountMismatchReportsDiagnostic()
    {
        const string testCode = """
            public interface IBase
            {
                int this[int x] { get; }
            }

            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                new int {|CSENSE026:this|}[int x, int y] { get; }
            }
            """;

        await VerifyCSenseAsync(testCode, diagnosticOptions: SuppressMissingDocs);
    }

    [Test]
    public async Task ImplicitInheritDocEnabledByDefault()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass
            {
                /// <summary>A virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>A derived class.</summary>
            public class MyClass : BaseClass
            {
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImplicitInheritDocDisabledReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass
            {
                /// <summary>A virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>A derived class.</summary>
            public class MyClass : BaseClass
            {
                public override void {|CSENSE018:M|}() { }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "false"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task ImplicitInheritDocEnabledButNotInheritingReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a class.</summary>
            public class MyClass
            {
                public void {|CSENSE001:M|}() { }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task ClassWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass { }

            /// <inheritdoc/>
            public class MyClass : BaseClass { }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InterfaceMemberWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base interface.</summary>
            public interface IBase
            {
                /// <summary>A method.</summary>
                void M();
            }

            /// <summary>A derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                void M();
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ClassWithInheritDocButNoBaseReportsDiagnostic()
    {
        const string testCode = """
            /// <inheritdoc/>
            public class {|CSENSE026:MyClass|} { }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task StructWithImplicitInheritDocReportsDiagnostic()
    {
        const string testCode = """
            public struct {|CSENSE001:MyStruct|} { }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task EnumWithImplicitInheritDocReportsDiagnostic()
    {
        const string testCode = """
            public enum {|CSENSE001:MyEnum|} { {|CSENSE001:A|} }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task DelegateWithImplicitInheritDocReportsDiagnostic()
    {
        const string testCode = """
            public delegate void {|CSENSE001:MyDelegate|}();
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task NestedInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass
            {
                /// <summary>A virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>A derived class.</summary>
            public class MyClass : BaseClass
            {
                /// <summary>
                /// <inheritdoc/>
                /// </summary>
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithLowQualitySummaryReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Provides a base implementation.</summary>
            public class BaseClass
            {
                /// <summary>A virtual method.</summary>
                public virtual void M() {}
            }

            /// <summary>A derived class.</summary>
            public class MyClass : BaseClass
            {
                /// <inheritdoc/>
                /// {|CSENSE016:<summary>M</summary>|}
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InterfaceMemberWithDifferentSignatureDoesNotInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                void M();
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                void {|CSENSE026:M|}(int x);
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task GenericMethodInInterfaceWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <typeparam name="T">Type T.</typeparam>
                /// <param name="x">Param x.</param>
                void M<T>(T x);
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                void M<T>(T x);
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PropertyInInterfaceWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Property P.</summary>
                /// <value>Value P.</value>
                int P { get; }
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                int P { get; }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InterfaceMemberWithDifferentReturnTypeDoesNotInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                void M();
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                int {|CSENSE026:M|}();
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InterfaceMemberWithDifferentRefKindDoesNotInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <param name="x">Param x.</param>
                void M(int x);
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                void {|CSENSE026:M|}(ref int x);
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InterfacePropertyWithDifferentTypeDoesNotInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Property P.</summary>
                /// <value>Value P.</value>
                int P { get; }
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                string {|CSENSE026:P|} { get; }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DeeplyNestedInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base class.</summary>
            public class BaseClass
            {
                /// <summary>Method M.</summary>
                public virtual void M() {}
            }

            /// <summary>Derived class.</summary>
            public class MyClass : BaseClass
            {
                /// <summary>
                /// <para>
                /// <inheritdoc/>
                /// </para>
                /// </summary>
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InterfaceEventWithInheritDocDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Event E.</summary>
                event EventHandler E;
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                event EventHandler E;
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InterfaceEventWithDifferentTypeDoesNotInheritDoc()
    {
        const string testCode = """
            using System;
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Event E.</summary>
                event EventHandler E;
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                event Action {|CSENSE026:E|};
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocInRemarksDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base class documentation.</summary>
            public class BaseClass
            {
                /// <summary>Base method documentation.</summary>
                public virtual void M() {}
            }

            /// <summary>Derived class documentation.</summary>
            public class MyClass : BaseClass
            {
                /// <summary>A more detailed summary for the override.</summary>
                /// <remarks><inheritdoc/></remarks>
                public override void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocInReturnsDoesNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base class documentation.</summary>
            public class BaseClass
            {
                /// <summary>Base method documentation.</summary>
                /// <returns>Returns a meaningful value.</returns>
                public virtual int M() => 0;
            }

            /// <summary>Derived class documentation.</summary>
            public class MyClass : BaseClass
            {
                /// <summary>A more detailed summary for the override.</summary>
                /// <returns><inheritdoc/></returns>
                public override int M() => 1;
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocOnNonInheritingMethodReportsDiagnostic()
    {
        // Issue 1: The check for invalid inheritdoc was short-circuiting because HasValidDocumentation returned true for inheritdoc.
        const string testCode = """
            /// <summary>Class Summary.</summary>
            public class MyClass
            {
                /// <inheritdoc/>
                public void {|CSENSE026:M|}() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocOnStaticMethodMatchingInterfaceNameReportsDiagnostic()
    {
        // Issue 2: IsInheriting might incorrectly identify this static method as implementing the interface member.
        // We use explicit implementation for the real interface member to allow the static method to exist with the same name.
        const string testCode = """
            /// <summary>Interface IBase.</summary>
            public interface IBase {
                /// <summary>Method M description.</summary>
                void M();
            }

            /// <summary>Class MyClass description.</summary>
            public class MyClass : IBase
            {
                void IBase.M() {}

                /// <inheritdoc/>
                public static void {|CSENSE026:M|}() { }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocOnRefReturnMethodMatchingValueReturnInterfaceReportsDiagnostic()
    {
        // Issue 2: IsInheriting might ignore ref return differences.
        const string testCode = """
            /// <summary>Interface IBase.</summary>
            public interface IBase {
                /// <summary>Method M description.</summary>
                /// <returns>Integer value.</returns>
                int M();
            }

            /// <summary>Class MyClass description.</summary>
            public class MyClass : IBase
            {
                int IBase.M() => 0;
                private static int _field;

                /// <inheritdoc/>
                public ref int {|CSENSE026:M|}() { return ref _field; }
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocOnStaticPropertyMatchingInterfaceNameReportsDiagnostic()
    {
        // Issue 2: Property check for IsInheriting.
        const string testCode = """
            /// <summary>Interface IBase.</summary>
            public interface IBase {
                /// <summary>Property P description.</summary>
                /// <value>Integer value.</value>
                int P { get; }
            }

            /// <summary>Class MyClass description.</summary>
            public class MyClass : IBase
            {
                int IBase.P => 0;

                /// <inheritdoc/>
                public static int {|CSENSE026:P|} => 0;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InheritDocOnRefReturnPropertyMatchingValueReturnInterfaceReportsDiagnostic()
    {
        // Issue 2: Property check for IsInheriting (Ref return).
        const string testCode = """
            /// <summary>Interface IBase.</summary>
            public interface IBase {
                /// <summary>Property P description.</summary>
                /// <value>Integer value.</value>
                int P { get; }
            }

            /// <summary>Class MyClass description.</summary>
            public class MyClass : IBase
            {
                int IBase.P => 0;
                private static int _field;

                /// <inheritdoc/>
                public static ref int {|CSENSE026:P|} => ref _field;
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ClassImplementingInterfaceWithoutDocsReportsMissingDocumentation()
    {
        // Types implementing interfaces do not implicitly inherit docs, so they should always report CSENSE001.
        const string testCode = """
            /// <summary>Interface.</summary>
            public interface IBase {}

            public class {|CSENSE001:MyClass|} : IBase {}
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ClassImplementingInterfaceWithImplicitDocsOptionEnabledStillReportsMissingDocumentation()
    {
        // Even with implicit docs enabled, types still require explicit documentation/inheritdoc.
        const string testCode = """
            /// <summary>Interface.</summary>
            public interface IBase {}

            public class {|CSENSE001:MyClass|} : IBase {}
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task ClassInheritingExceptionShouldReportMissingDocsNotMissingInheritDoc()
    {
        const string testCode = """
            using System;
            public class {|CSENSE001:MyException|} : Exception
            {
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ClassInheritingListShouldReportMissingDocsNotMissingInheritDoc()
    {
        const string testCode = """
            using System.Collections.Generic;
            public class {|CSENSE001:MyList|} : List<int>
            {
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ClassInheritingComponentShouldReportMissingDocsNotMissingInheritDoc()
    {
        const string testCode = """
            /// <summary>Base.</summary>
            public class ComponentBase { }

            public class {|CSENSE001:MyComponent|} : ComponentBase
            {
            }
            """;
        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task DerivedClassWithImplicitDocOptionShouldStillRequireDocs()
    {
        const string testCode = """
            /// <summary>Base</summary>
            public class BaseClass { }

            public class {|CSENSE001:DerivedClass|} : BaseClass { }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task FieldInInterfaceShouldNotBeConsideredInheriting()
    {
        const string testCode = """
            /// <summary>Base</summary>
            public interface IBase
            {
                /// <summary>Field</summary>
                static int F = 0;
            }

            /// <summary>Derived</summary>
            public interface IDerived : IBase
            {
                static int {|CSENSE001:F|} = 1;
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task GenericTypeParamPropertyShouldBeDetectedAsInheriting()
    {
        const string testCode = """
            /// <summary>Provides a base generic contract.</summary>
            /// <typeparam name="T">The type of the value.</typeparam>
            public interface IBase<T>
            {
                /// <summary>Gets the value of type T.</summary>
                /// <value>A value of type T.</value>
                T P { get; }
            }

            /// <summary>Provides a derived generic contract.</summary>
            /// <typeparam name="U">The type of the derived value.</typeparam>
            public interface IDerived<U> : IBase<U>
            {
                U P { get; }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "true"
        };

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: options);
    }

    [Test]
    public async Task InterfaceInheritingInterfaceWithInheritDocShouldNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase { }

            /// <inheritdoc/>
            public interface IDerived : IBase { }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ClassImplementingInterfaceWithInheritDocShouldNotReportDiagnostic()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase { }

            /// <inheritdoc/>
            public class MyClass : IBase { }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImplicitInterfaceImplementationEnabledByDefault()
    {
        const string testCode = """
            /// <summary>Interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                void M();
            }

            /// <summary>Class.</summary>
            public class MyClass : IBase
            {
                public void M() { }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImplicitInterfaceImplementationDisabledReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>Interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                void M();
            }

            /// <summary>Class.</summary>
            public class MyClass : IBase
            {
                public void {|CSENSE018:M|}() { }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.allow_implicit_inheritdoc"] = "false"
        };

        await VerifyCSenseAsync(testCode, configOptions: options);
    }

    [Test]
    public async Task ImplicitInterfaceEventImplementationEnabledByDefault()
    {
        const string testCode = """
            using System;
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Event E.</summary>
                event EventHandler E;
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                event EventHandler E;
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImplicitInterfaceMethodWithDifferentParameterCountReportsMissingDocumentation()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <param name="x">Input parameter.</param>
                void M(int x);
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                void {|CSENSE001:M|}();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ImplicitGenericInterfaceMethodImplementationEnabledByDefault()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <typeparam name="T">Type parameter.</typeparam>
                /// <param name="value">Value parameter.</param>
                void M<T>(T value);
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                void M<T>(T value);
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ImplicitInterfaceMethodWithDifferentReturnTypeReportsMissingDocumentation()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <returns>Integer value.</returns>
                int M();
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                string {|CSENSE001:M|}();
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InterfaceMemberWithCovariantReturnTypeShouldInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Method M.</summary>
                /// <returns>An object.</returns>
                object M();
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                new string M();
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InterfacePropertyWithCovariantReturnTypeShouldInheritDoc()
    {
        const string testCode = """
            /// <summary>Base interface.</summary>
            public interface IBase
            {
                /// <summary>Property P.</summary>
                /// <value>A highly descriptive value description.</value>
                object P { get; }
            }

            /// <summary>Derived interface.</summary>
            public interface IDerived : IBase
            {
                /// <inheritdoc/>
                new string P { get; }
            }
            """;
        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
