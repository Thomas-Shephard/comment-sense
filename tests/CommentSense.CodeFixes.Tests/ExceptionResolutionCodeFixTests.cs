using CommentSense.Analyzers;
using CommentSense.CodeFixes.Logic;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis.Testing;
using NUnit.Framework;

namespace CommentSense.CodeFixes.Tests;

public class ExceptionResolutionCodeFixTests : CommentSenseCodeFixTestBase<CommentSenseAnalyzer, ExceptionResolutionCodeFixProvider>
{
    [Test]
    public async Task FullyQualifiedNameFix()
    {
        const string testCode = """
            using System;

            namespace Other
            {
                /// <summary>Other exception.</summary>
                public class MyOtherException : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="{|CSENSE007:MyOtherException|}">Not in scope.</exception>
                    public void MyMethod()
                    {
                    }
                }
            }
            """;

        const string fixedCode = """
            using System;

            namespace Other
            {
                /// <summary>Other exception.</summary>
                public class MyOtherException : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="Other.MyOtherException">Not in scope.</exception>
                    public void MyMethod()
                    {
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task UnresolvedCrefFix()
    {
        const string testCode = """
            using System;

            namespace Other
            {
                /// <summary>Some exception.</summary>
                public class MySomeException : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="{|CSENSE007:MySomeException|}">Misspelled or missing namespace.</exception>
                    public void MyMethod()
                    {
                        throw new Other.MySomeException();
                    }
                }
            }
            """;

        const string fixedCode = """
            using System;

            namespace Other
            {
                /// <summary>Some exception.</summary>
                public class MySomeException : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="Other.MySomeException">Misspelled or missing namespace.</exception>
                    public void MyMethod()
                    {
                        throw new Other.MySomeException();
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task DisabledThresholdFixNotOffered()
    {
        const string testCode = """
            using System;

            /// <summary>Main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE007:ArgumetNullException|}">Typo in cref.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyNoCodeFixAsync(testCode, configOptions: new Dictionary<string, string>
        {
            ["comment_sense.rename_similarity_threshold"] = "0.0"
        });
    }

    [Test]
    public async Task FuzzyCrefMatchFix()
    {
        const string testCode = """
            using System;

            /// <summary>Main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE007:ArgumetNullException|}">Typo in cref.</exception>
                /// <exception cref="InvalidOperationException">Correct.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        const string fixedCode = """
            using System;

            /// <summary>Main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="System.ArgumentNullException">Typo in cref.</exception>
                /// <exception cref="InvalidOperationException">Correct.</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task InvalidTypeFix()
    {
        const string testCode = """
            using System;

            /// <summary>Not an exception class.</summary>
            public class MyNotAnException { }

            /// <summary>A real exception class.</summary>
            public class MyException : Exception { }

            /// <summary>Test class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE017:MyNotAnException|}">Not an exception.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new MyException();
                }
            }
            """;

        const string fixedCode = """
            using System;

            /// <summary>Not an exception class.</summary>
            public class MyNotAnException { }

            /// <summary>A real exception class.</summary>
            public class MyException : Exception { }

            /// <summary>Test class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="MyException">Not an exception.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new MyException();
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task InferredExceptionFix()
    {
        const string testCode = """
            using System;

            /// <summary>Main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="{|CSENSE007:ArgNullException|}">Misspelled.</exception>
                public void {|CSENSE012:MyMethod|}()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        const string fixedCode = """
            using System;

            /// <summary>Main class.</summary>
            public class MyClass
            {
                /// <summary>This is a summary for the method.</summary>
                /// <exception cref="System.ArgumentNullException">Misspelled.</exception>
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task InvalidSymbolFix()
    {
        const string testCode = """
            using System;

            /// <summary>Test class.</summary>
            public class MyClass
            {
                /// <summary>This handles empty.</summary>
                public void ArgumentNull() {}

                /// <summary>This summary is definitely long enough and unique to avoid any quality warnings from being reported by the analyzer and does not contain any forbidden words like the one for nothingness.</summary>
                /// <exception cref="{|CSENSE017:ArgumentNull|}">Reference to a method.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        const string fixedCode = """
            using System;

            /// <summary>Test class.</summary>
            public class MyClass
            {
                /// <summary>This handles empty.</summary>
                public void ArgumentNull() {}

                /// <summary>This summary is definitely long enough and unique to avoid any quality warnings from being reported by the analyzer and does not contain any forbidden words like the one for nothingness.</summary>
                /// <exception cref="System.ArgumentNullException">Reference to a method.</exception>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE012")]
                public void MyMethod()
                {
                    throw new ArgumentNullException();
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }

    [Test]
    public async Task GenericExceptionFix()
    {
        const string testCode = """
            using System;

            namespace Other
            {
                /// <summary>Generic exception.</summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE004")]
                public class MyGenericException<T> : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="{|CSENSE007:MyGenericException|}">Generic missing.</exception>
                    public void MyMethod()
                    {
                        throw new Other.MyGenericException<int>();
                    }
                }
            }
            """;

        const string fixedCode = """
            using System;

            namespace Other
            {
                /// <summary>Generic exception.</summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("CommentSense", "CSENSE004")]
                public class MyGenericException<T> : Exception { }
            }

            namespace Main
            {
                /// <summary>Main class.</summary>
                public class MyClass
                {
                    /// <summary>This is a summary for the method.</summary>
                    /// <exception cref="Other.MyGenericException{System.Int32}">Generic missing.</exception>
                    public void MyMethod()
                    {
                        throw new Other.MyGenericException<int>();
                    }
                }
            }
            """;

        await VerifyCodeFixAsync(testCode, fixedCode);
    }
}
