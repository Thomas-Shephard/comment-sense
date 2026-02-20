using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class AdvancedExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task IgnoreSystemExceptionsSuppressesDiagnostics()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string> { ["comment_sense.ignore_system_exceptions"] = "true" }, expectDiagnostic: false);
    }

    [Test]
    public async Task IgnoredExceptionNamespacesSuppressesDiagnostics()
    {
        const string testCode = """
            namespace Custom.Namespace
            {
                internal class CustomException : System.Exception {}
            }

            namespace Test
            {
                using Custom.Namespace;
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void MyMethod()
                    {
                        throw new CustomException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exception_namespaces"] = "Custom.Namespace" }, expectDiagnostic: false);
    }

    [Test]
    public async Task IgnoredExceptionNamespacesWithMultipleEntriesSuppressesDiagnostics()
    {
        const string testCode = """
            namespace Custom.Namespace1
            {
                internal class CustomException1 : System.Exception {}
            }
            namespace Custom.Namespace2
            {
                internal class CustomException2 : System.Exception {}
            }

            namespace Test
            {
                using Custom.Namespace1;
                using Custom.Namespace2;
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void MyMethod()
                    {
                        throw new CustomException1();
                        throw new CustomException2();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exception_namespaces"] = "Custom.Namespace1, Custom.Namespace2" }, expectDiagnostic: false);
    }

    [Test]
    public async Task NonIgnoredNamespaceStillReportsDiagnostic()
    {
        const string testCode = """
            namespace Custom.Namespace
            {
                internal class CustomException : System.Exception {}
            }

            namespace Other.Namespace
            {
                internal class OtherException : System.Exception {}
            }

            namespace Test
            {
                using Custom.Namespace;
                using Other.Namespace;
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void {|CSENSE012:MyMethod|}()
                    {
                        throw new CustomException();
                        throw new OtherException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exception_namespaces"] = "Custom.Namespace" });
    }

    [Test]
    public async Task ConstrainedGenericExceptionDoesNotCrash()
    {
        const string testCode = """
            using System;
            /// <summary>This is a summary for the class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <typeparam name="T">The exception type.</typeparam>
                public void {|CSENSE012:MyMethod|}<T>() where T : Exception, new()
                {
                    throw new T();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: true);
    }

    [Test]
    public async Task SubNamespaceIsIgnored()
    {
        const string testCode = """
            namespace MyProject.Internal.Sub
            {
                internal class SubException : System.Exception {}
            }

            namespace Test
            {
                using MyProject.Internal.Sub;
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void MyMethod()
                    {
                        throw new SubException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string> { ["comment_sense.ignored_exception_namespaces"] = "MyProject.Internal" }, expectDiagnostic: false);
    }

    [Test]
    public async Task NamespaceChecksAreHit()
    {
        const string testCode = """
            namespace MyNamespace
            {
                internal class MyException : System.Exception {}
                /// <summary>This is a summary for the class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    public void {|CSENSE012:MyMethod|}()
                    {
                        throw new MyException();
                    }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string>
        {
            ["comment_sense.ignore_system_exceptions"] = "true",
            ["comment_sense.ignored_exception_namespaces"] = "OtherNamespace"
        }, expectDiagnostic: true);
    }
}
