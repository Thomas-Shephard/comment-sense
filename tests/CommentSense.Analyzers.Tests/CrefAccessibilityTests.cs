using CommentSense.TestHelpers;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class CrefAccessibilityTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task PublicMemberPointingToInternalTypeReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                /// <see cref="{|CSENSE025:InternalType|}"/>
                public class PublicClass { }

                /// <summary>Internal type.</summary>
                internal class InternalType { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberPointingToPrivateMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:PrivateMethod|}"/>
                    /// </summary>
                    public void PublicMethod() { }

                    /// <summary>Private method.</summary>
                    private void PrivateMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberPointingToInternalMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:InternalMethod|}"/>
                    /// </summary>
                    public void PublicMethod() { }

                    /// <summary>Internal method.</summary>
                    internal void InternalMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberPointingToProtectedMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:ProtectedMethod|}"/>
                    /// </summary>
                    public void PublicMethod() { }

                    /// <summary>Protected method.</summary>
                    protected void ProtectedMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task ProtectedMemberPointingToInternalMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:InternalMethod|}"/>
                    /// </summary>
                    protected void ProtectedMethod() { }

                    /// <summary>Internal method.</summary>
                    internal void InternalMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task InternalMemberPointingToPrivateMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:PrivateMethod|}"/>
                    /// </summary>
                    internal void InternalMethod() { }

                    /// <summary>Private method.</summary>
                    private void PrivateMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, configOptions: new Dictionary<string, string>
        {
            { "comment_sense.visibility_level", "internal" }
        });
    }

    [Test]
    public async Task InternalMemberPointingToInternalMemberDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="OtherInternalMethod"/>
                    /// </summary>
                    internal void InternalMethod() { }

                    /// <summary>Other internal method.</summary>
                    internal void OtherInternalMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PrivateMemberPointingToPrivateMemberDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="OtherPrivateMethod"/>
                    /// </summary>
                    private void PrivateMethod() { }

                    /// <summary>Other private method.</summary>
                    private void OtherPrivateMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PublicMemberPointingToPublicMemberDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="OtherPublicMethod"/>
                    /// </summary>
                    public void PublicMethod() { }

                    /// <summary>Other public method.</summary>
                    public void OtherPublicMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InternalClassMemberPointingToInternalClassTypeDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Internal class.</summary>
                internal class InternalClass
                {
                    /// <summary>
                    /// See <see cref="OtherInternalClass"/>
                    /// </summary>
                    public void PublicMethod() { }
                }

                /// <summary>Other internal class.</summary>
                internal class OtherInternalClass { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ProtectedInternalMemberPointingToProtectedMemberDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="ProtectedMethod"/>
                    /// </summary>
                    protected internal void ProtectedInternalMethod() { }

                    /// <summary>Protected method.</summary>
                    protected void ProtectedMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task ProtectedMemberPointingToPrivateProtectedMemberReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:PrivateProtectedMethod|}"/>
                    /// </summary>
                    protected void ProtectedMethod() { }

                    /// <summary>Private protected method.</summary>
                    private protected void PrivateProtectedMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PrivateProtectedMemberPointingToInternalMemberDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="InternalMethod"/>
                    /// </summary>
                    private protected void PrivateProtectedMethod() { }

                    /// <summary>Internal method.</summary>
                    internal void InternalMethod() { }
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false, configOptions: new Dictionary<string, string>
        {
            { "comment_sense.visibility_level", "internal" }
        });
    }

    [Test]
    public async Task PublicMemberPointingToInternalArrayReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                /// <see cref="{|CSENSE025:InternalType|}[]"/>
                public class PublicClass { }

                /// <summary>Internal type.</summary>
                internal class InternalType { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberPointingToInternalPointerReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                public class PublicClass
                {
                    /// <summary>
                    /// See <see cref="{|CSENSE025:InternalType|}*"/>
                    /// </summary>
                    public unsafe void Method() { }
                }

                /// <summary>Internal type.</summary>
                internal class InternalType { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberPointingToGenericWithInternalArgumentReportsDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Public class.</summary>
                /// <see cref="{|CSENSE025:GenericType{MyNamespace.InternalType}|}"/>
                public class PublicClass { }

                /// <summary>Internal type.</summary>
                internal class InternalType { }

                /// <summary>Public generic type.</summary>
                /// <typeparam name="T">The type parameter.</typeparam>
                public class GenericType<T> { }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task PublicMemberInInternalClassPointingToInternalTypeDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                internal class InternalOuter
                {
                    /// <summary>
                    /// See <see cref="InternalOther"/>
                    /// </summary>
                    public void PublicMethod() { }
                }

                internal class InternalOther { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task PublicMemberInNestedClassHierarchyDoesNotReportDiagnostic()
    {
        const string testCode = """
            namespace MyNamespace
            {
                /// <summary>Outer class.</summary>
                public class PublicOuter
                {
                    internal class InternalInner
                    {
                        public class PublicNested
                        {
                             /// <summary>
                             /// See <see cref="InternalOther"/>
                             /// </summary>
                             public void PublicMethod() { }
                        }
                    }
                }

                internal class InternalOther { }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }
}
