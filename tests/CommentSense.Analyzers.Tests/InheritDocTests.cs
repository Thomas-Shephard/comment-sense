using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InheritDocTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task InvalidInheritDocReportsDiagnostic()
    {
        const string testCode = """
            /// <summary>This is a class.</summary>
            public class MyClass
            {
                /// <inheritdoc/>
                public void {|CSENSE001:MyMethod|}() { }
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
            public class {|CSENSE001:MyClass|} { }
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
                void {|CSENSE001:M|}(int x);
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
                int {|CSENSE001:M|}();
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
                void {|CSENSE001:M|}(ref int x);
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
                string {|CSENSE001:P|} { get; }
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
                event Action {|CSENSE001:E|};
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
                public void {|CSENSE001:M|}() { }
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
                public static void {|CSENSE001:M|}() { }
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
                public ref int {|CSENSE001:M|}() { return ref _field; }
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
                public static int {|CSENSE001:P|} => 0;
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
                public static ref int {|CSENSE001:P|} => ref _field;
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

    private static readonly Dictionary<string, string> BaseOptions = new()
    {
        ["dotnet_diagnostic.CSENSE001.severity"] = "none",
        ["dotnet_diagnostic.CSENSE002.severity"] = "none",
        ["dotnet_diagnostic.CSENSE007.severity"] = "none",
        ["dotnet_diagnostic.CSENSE014.severity"] = "none",
        ["dotnet_diagnostic.CSENSE016.severity"] = "none"
    };

    private static readonly Dictionary<string, string> EnableScanning = new(BaseOptions)
    {
        ["comment_sense.scan_called_methods_for_exceptions"] = "true"
    };

    [Test]
    public async Task InheritDocWithNewExceptionWhenScanningDisabledIsReported()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public virtual void MyMethod() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                public override void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: BaseOptions);
    }

    [Test]
    public async Task InheritDocWithNewExceptionWhenScanningEnabledReportsDiagnostic()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public virtual void MyMethod() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                public override void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException(); // Inherited
                    throw new ArgumentException(); // New
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithNewExceptionDocumentedInDerivedWhenScanningEnabledIsSatisfied()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public virtual void MyMethod() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                /// <exception cref="ArgumentException">This is a high quality description for the new exception.</exception>
                public override void MyMethod()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithCrefWhenScanningEnabledVerifiesAgainstCrefTarget()
    {
        const string testCode = """
            using System;
            /// <summary>Other class</summary>
            public class Other
            {
                /// <summary>External method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public void External() { }
            }
            /// <summary>My class</summary>
            public class MyClass
            {
                /// <inheritdoc cref="Other.External"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException(); // OK
                    throw new ArgumentException(); // New
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocRecursiveWhenScanningEnabledFindsBaseExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public virtual void M() { }
            }
            /// <summary>Middle class</summary>
            public class Middle : Base
            {
                /// <inheritdoc/>
                public override void M() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Middle
            {
                /// <inheritdoc/>
                public override void {|CSENSE012:M|}()
                {
                    if (true) throw new InvalidOperationException(); // Inherited from Base
                    throw new ArgumentException(); // New
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocInterfaceWhenScanningEnabledFindsInterfaceExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>Interface</summary>
            public interface I
            {
                /// <summary>Method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                void M();
            }
            /// <summary>Implementation class</summary>
            public class C : I
            {
                /// <inheritdoc/>
                public void {|CSENSE012:M|}()
                {
                    if (true) throw new InvalidOperationException(); // Inherited from I
                    throw new ArgumentException(); // New
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocPropertyWhenScanningEnabledFindsBaseExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base property</summary>
                /// <value>Some value</value>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public virtual int MyProperty => throw new InvalidOperationException();
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                /// <value>Some value</value>
                public override int {|CSENSE012:MyProperty|} => throw new ArgumentException();
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocConstructorCrefReportsNewException()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
            public class Base
            {
                /// <summary>Base constructor</summary>
                /// <param name="x">The value</param>
                public Base(int x) {}
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc cref="Base(int)"/>
                public {|CSENSE012:Derived|}(int x) : base(x)
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocTypeCrefInheritsTypeExceptions()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            /// <exception cref="InvalidOperationException">High quality description.</exception>
            public class Base { }

            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="Base"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException(); // Inherited
                    throw new ArgumentException(); // New
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithInvalidCrefDoesNotCrash()
    {
        const string testCode = """
            using System;
            /// <summary>My class</summary>
            public class MyClass
            {
                /// <inheritdoc cref="NonExistent"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithBaseMemberMissingDocumentationDoesNotCrash()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                public virtual void MyMethod() { }
            }
            /// <summary>Derived class</summary>
            public class Derived : Base
            {
                /// <inheritdoc/>
                public override void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithExplicitInterfaceImplementation()
    {
        const string testCode = """
            using System;
            /// <summary>Interface</summary>
            public interface I
            {
                /// <summary>Method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                void M();
            }
            /// <summary>Implementation class</summary>
            public class C : I
            {
                /// <inheritdoc/>
                void I.M()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithCircularReferenceDoesNotCrash()
    {
        const string testCode = """
            using System;
            /// <summary>Class A</summary>
            public class A
            {
                /// <inheritdoc cref="B.M"/>
                public virtual void M() { }
            }
            /// <summary>Class B</summary>
            public class B : A
            {
                /// <inheritdoc cref="A.M"/>
                public override void M() { }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithMultiplePathsToSameBase()
    {
        const string testCode = """
            using System;
            /// <summary>Interface 1</summary>
            public interface I1
            {
                /// <summary>Method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                void M();
            }
            /// <summary>Interface 2</summary>
            public interface I2 : I1
            {
                /// <inheritdoc/>
                new void M();
            }
            /// <summary>Implementation class</summary>
            public class C : I1, I2
            {
                /// <inheritdoc/>
                public void {|CSENSE012:M|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithTypeCref()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
            public class Base { }

            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="Base"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocRecursiveWithCref()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">This is a high quality description for the exception.</exception>
                public void M1() { }
            }
            /// <summary>Middle class</summary>
            public class Middle
            {
                /// <inheritdoc cref="Base.M1"/>
                public void M2() { }
            }
            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="Middle.M2"/>
                public void {|CSENSE012:M3|}()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithExplicitMethodPrefix()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                public void M1() { }
            }
            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="M:Base.M1"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithExplicitPropertyPrefix()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base property</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                public int P1 => 0;
            }
            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="P:Base.P1"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithExplicitTypePrefix()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            /// <exception cref="InvalidOperationException">High quality description.</exception>
            public class Base { }

            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="T:Base"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithPropertyCrefNoPrefix()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base property</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                public int P1 => 0;
            }
            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="Base.P1"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocStrayWithIncludeDoesNotBypassAutoValidCheck()
    {
        const string testCode = """
            using System;
            /// <summary>My class</summary>
            public class MyClass
            {
                /// <remarks>
                /// <inheritdoc/>
                /// </remarks>
                /// <include file='docs.xml' path='[@name="M"]/*'/>
                public void MyMethod()
                {
                    throw new ArgumentException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithWhitespaceInCref()
    {
        const string testCode = """
            using System;
            /// <summary>Base class</summary>
            public class Base
            {
                /// <summary>Base method</summary>
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                public void M1() { }
            }
            /// <summary>Derived class</summary>
            public class Derived
            {
                /// <inheritdoc cref="
                ///     Base.M1
                /// "/>
                public void MyMethod()
                {
                    if (true) throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithInvalidCrefReportsCSense012()
    {
        const string testCode = """
            using System;
            /// <summary>My class</summary>
            public class MyClass
            {
                /// <inheritdoc cref="NonExistent.Method"/>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocOnEvent()
    {
        const string testCode = """
            using System;
            public class Base {
                public virtual event EventHandler MyEvent;
            }
            public class Derived : Base {
                /// <inheritdoc/>
                public override event EventHandler MyEvent;
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithShortCref()
    {
        const string testCode = """
            using System;
            /// <summary>Base</summary>
            /// <exception cref="InvalidOperationException">High quality description.</exception>
            public class C { }

            /// <summary>Derived</summary>
            public class D : C {
                /// <inheritdoc cref="C"/>
                public void M() {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithDiamondInheritance()
    {
        const string testCode = """
            using System;
            public interface IBase {
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                void M();
            }
            public interface IA : IBase {
                /// <inheritdoc/>
                new void M();
            }
            public interface IB : IBase {
                /// <inheritdoc/>
                new void M();
            }
            public class Derived : IA, IB {
                /// <inheritdoc/>
                public void M() {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithUnresolvableCref()
    {
        const string testCode = """
            using System;
            public class Derived {
                /// <inheritdoc cref="Definitely.Not.Exists"/>
                public void {|CSENSE012:M|}() {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithMethodCrefNoPrefix()
    {
        const string testCode = """
            using System;
            public class Base {
                /// <exception cref="InvalidOperationException">High quality description.</exception>
                public virtual void M() { }
            }
            public class Derived : Base {
                /// <inheritdoc cref="Base.M"/>
                public override void {|CSENSE012:M|}() {
                    if (true) throw new InvalidOperationException();
                    throw new ArgumentException();
                }
            }
            """;
        await VerifyCSenseAsync(testCode, configOptions: EnableScanning);
    }

    [Test]
    public async Task InheritDocWithMetadataInclude()
    {
        const string testCode = """
            using System;
            namespace BaseLib {
                /// <summary>Base class</summary>
                public class Base {
                    /// <summary>Base method</summary>
                    /// <exception cref="InvalidOperationException">High quality description.</exception>
                    public virtual void M() { }
                }

                /// <summary>Derived class</summary>
                public class Derived : Base
                {
                    /// <inheritdoc/>
                    public override void M()
                    {
                        if (true) throw new InvalidOperationException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: EnableScanning,
            expectDiagnostic: false);
    }
}
