using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class ExternalMethodExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task CallDocumentedMethodReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Does something.</summary>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public void DoSomething() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    other.DoSomething();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallDocumentedMethodWithHandleDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Does something.</summary>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public void DoSomething() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void MyMethod(OtherClass other)
                {
                    try
                    {
                        other.DoSomething();
                    }
                    catch (InvalidOperationException)
                    {
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallDocumentedMethodWithDocumentationDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Does something.</summary>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public void DoSomething() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                /// <exception cref="InvalidOperationException">Rethrown.</exception>
                public void MyMethod(OtherClass other)
                {
                    other.DoSomething();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallDocumentedConstructorReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Initializes a new instance.</summary>
                /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
                public OtherClass() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    new OtherClass();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallDocumentedImplicitConstructorReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Initializes a new instance.</summary>
                /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
                public OtherClass() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    OtherClass other = new();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallStandardLibraryMethodReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="s">The string.</param>
                public void {|#0:MyMethod|}(string s)
                {
                    int.Parse(s);
                }
            }
            """;

        // int.Parse(string) throws ArgumentNullException, FormatException, OverflowException.
        var expected1 = new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule)
            .WithLocation(0)
            .WithArguments("ArgumentNullException");
        var expected2 = new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule)
            .WithLocation(0)
            .WithArguments("FormatException");
        var expected3 = new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule)
            .WithLocation(0)
            .WithArguments("OverflowException");

        await VerifyCSenseAsync(testCode,
            expectedDiagnostics: [expected1, expected2, expected3],
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task AccessDocumentedPropertyReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Some property.</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public int SomeProperty => 42;
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    var x = other.SomeProperty;
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task AccessDocumentedIndexerReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Some indexer.</summary>
                /// <param name="index">The index.</param>
                /// <value>The value.</value>
                /// <exception cref="IndexOutOfRangeException">Thrown when something is wrong.</exception>
                public int this[int index] => 42;
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    var x = other[0];
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallMethodOnDocumentedPropertyReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>This is the other class.</summary>
            public class Other { 
                /// <summary>This does something else.</summary>
                public void M() {} 
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Some property.</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public Other Prop => null;

                /// <summary>This is a summary for the method.</summary>
                public void {|CSENSE012:MyMethod|}()
                {
                    Prop.M();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallMethodOnDocumentedPropertyChainReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>This is a deep class.</summary>
            public class Deep { 
                /// <summary>This does a deep thing.</summary>
                public void M() {} 
            }

            /// <summary>This is the other class.</summary>
            public class Other 
            {
                /// <summary>Deep property.</summary>
                /// <value>The value.</value>
                /// <exception cref="NotSupportedException">Thrown when something is wrong.</exception>
                public Deep DeepProp => null;
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>Some property.</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public Other Prop => null;

                /// <summary>This is a summary for the method.</summary>
                public void {|#0:MyMethod|}()
                {
                    Prop.DeepProp.M();
                }
            }
            """;

        var expected1 = new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule)
            .WithLocation(0)
            .WithArguments("InvalidOperationException");
        var expected2 = new DiagnosticResult(CommentSenseRules.MissingExceptionDocumentationRule)
            .WithLocation(0)
            .WithArguments("NotSupportedException");

        await VerifyCSenseAsync(testCode,
            expectedDiagnostics: [expected1, expected2],
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task CallDocumentedDelegateReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>My delegate.</summary>
            /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
            public delegate void MyDelegate();

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="d">The delegate.</param>
                public void {|CSENSE012:MyMethod|}(MyDelegate d)
                {
                    d();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ConditionalMemberAccessReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Some property.</summary>
                /// <value>The value.</value>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public int SomeProperty => 42;
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    var x = other?.SomeProperty;
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ConditionalElementAccessReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Some indexer.</summary>
                /// <param name="index">The index.</param>
                /// <value>The value.</value>
                /// <exception cref="IndexOutOfRangeException">Thrown when something is wrong.</exception>
                public int this[int index] => 42;
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    var x = other?[0];
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ConstructorInitializerReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Base class.</summary>
            public class BaseClass
            {
                /// <summary>Initializes a new instance.</summary>
                /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
                public BaseClass() { }
            }

            /// <summary>My class.</summary>
            public class MyClass : BaseClass
            {
                /// <summary>Initializes a new instance.</summary>
                public {|CSENSE012:MyClass|}() : base() { }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ImplicitObjectCreationWithScanningDisabledDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Initializes a new instance.</summary>
                /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
                public OtherClass() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    OtherClass other = new();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "false" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ObjectCreationWithScanningDisabledDoesNotReportDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Initializes a new instance.</summary>
                /// <exception cref="ArgumentException">Thrown when something is wrong.</exception>
                public OtherClass() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    new OtherClass();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            expectDiagnostic: false,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "false" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }

    [Test]
    public async Task ConditionalMethodInvocationReportsDiagnostic()
    {
        const string testCode = """
            using System;

            /// <summary>Other class.</summary>
            public class OtherClass
            {
                /// <summary>Does something.</summary>
                /// <exception cref="InvalidOperationException">Thrown when something is wrong.</exception>
                public void DoSomething() { }
            }

            /// <summary>My class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <param name="other">The other.</param>
                public void {|CSENSE012:MyMethod|}(OtherClass other)
                {
                    other?.DoSomething();
                }
            }
            """;

        await VerifyCSenseAsync(testCode,
            configOptions: new Dictionary<string, string> { ["comment_sense.scan_called_methods_for_exceptions"] = "true" },
            referenceAssemblies: ReferenceAssemblies.Net.Net100);
    }
}
