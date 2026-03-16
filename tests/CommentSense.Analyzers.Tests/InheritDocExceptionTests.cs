using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class InheritDocExceptionTests : CommentSenseAnalyzerTestBase<CommentSenseAnalyzer>
{
    [Test]
    public async Task InheritDocWithoutCrefUsesInterfaceExceptionDocumentation()
    {
        const string testCode = """
            using System;

            /// <summary>Base contract.</summary>
            public interface IBase
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                void Execute();
            }

            /// <summary>Implementation.</summary>
            public class Worker : IBase
            {
                /// <inheritdoc/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocWithCrefUsesReferencedMethodExceptionDocumentation()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <summary>Source docs.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                public void Source() { }

                /// <inheritdoc cref="Source"/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocElementWithCrefUsesReferencedMethodExceptionDocumentation()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <summary>Source docs.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                public void Source() { }

                /// <inheritdoc cref="Source"></inheritdoc>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task RecursiveInheritDocCrefResolvesExceptionDocumentationTransitively()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <summary>Root docs.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                public void Root() { }

                /// <inheritdoc cref="Root"/>
                public void Middle() { }

                /// <inheritdoc cref="Middle"/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task CircularInheritDocCrefReportsMissingException()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <inheritdoc cref="B"/>
                public void A() { }

                /// <inheritdoc cref="A"/>
                public void B() { }

                /// <inheritdoc cref="A"/>
                public void {|CSENSE012:Execute|}()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode);
    }

    [Test]
    public async Task IncludeTagSkipsExceptionDocumentationRule()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <include file="Docs.xml" path="/doc/members/member[@name='M:Worker.Execute']/*"/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task InheritDocCrefTargetWithIncludeSkipsExceptionValidation()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <include file="Docs.xml" path="/doc/members/member[@name='M:Worker.Source']/*"/>
                public void Source() { }

                /// <inheritdoc cref="Source"/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task IncludeInInheritedInheritDocChainSkipsExceptionValidation()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <include file="Docs.xml" path="/doc/members/member[@name='M:Worker.Source']/*"/>
                public void Source() { }

                /// <inheritdoc cref="Source"/>
                public void Middle() { }

                /// <inheritdoc cref="Middle"/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MetadataInheritDocTargetWithoutResolvableChainSkipsExceptionValidation()
    {
        const string testCode = """
            using System;
            using RefLib;

            /// <summary>Type.</summary>
            public class Worker : Middle
            {
                /// <inheritdoc/>
                public override void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        const string referencedCode = """
            using System;

            namespace RefLib;

            /// <summary>Base type.</summary>
            public class Base
            {
                /// <summary>Executes operation.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                public virtual void Execute() { }
            }

            /// <summary>Middle type.</summary>
            public class Middle : Base
            {
                /// <inheritdoc/>
                public override void Execute() { }
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            expectDiagnostic: false,
            solutionTransform: (solution, projectId) =>
            {
                var project = solution.GetProject(projectId) ?? throw new InvalidOperationException();
                var referencedProjectId = ProjectId.CreateNewId();
                solution = solution.AddProject(referencedProjectId, "RefLib", "RefLib", LanguageNames.CSharp);
                solution = solution.AddMetadataReferences(referencedProjectId, project.MetadataReferences);
                solution = solution.WithProjectParseOptions(referencedProjectId, project.ParseOptions ?? throw new InvalidOperationException());
                solution = solution.WithProjectCompilationOptions(referencedProjectId, project.CompilationOptions ?? throw new InvalidOperationException());
                solution = solution.AddDocument(DocumentId.CreateNewId(referencedProjectId), "RefLib.cs", referencedCode);
                solution = solution.AddProjectReferences(projectId, [new ProjectReference(referencedProjectId)]);
                return solution;
            });
    }

    [Test]
    public async Task PartialTypeWithUndocumentedDeclarationStillResolvesInheritedExceptions()
    {
        const string testCode = """
            using System;

            /// <summary>Base docs.</summary>
            /// <exception cref="InvalidOperationException">Thrown by derived initialization.</exception>
            public class BaseType { }

            /// <inheritdoc/>
            public partial class Worker(int value) : BaseType
            {
                private readonly int _x = value > 0 ? value : throw new InvalidOperationException();
            }

            public partial class Worker
            {
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task MultipleMembersCanInheritExceptionsFromSameCrefTarget()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <summary>Source docs.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                public void Source() { }

                /// <inheritdoc cref="Source"/>
                public void ExecuteA()
                {
                    throw new InvalidOperationException();
                }

                /// <inheritdoc cref="Source"/>
                public void ExecuteB()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(testCode, expectDiagnostic: false);
    }

    [Test]
    public async Task NestedUnresolvableInheritDocCrefStillReportsMissingException()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public class Worker
            {
                /// <inheritdoc cref="Missing"/>
                public void Proxy() { }

                /// <inheritdoc cref="Proxy"/>
                public void {|CSENSE012:Execute|}()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            diagnosticOptions:
            [
                (CommentSenseDiagnosticIds.InvalidInheritDocTargetId, ReportDiagnostic.Suppress),
                (CommentSenseDiagnosticIds.UnresolvedCrefId, ReportDiagnostic.Suppress)
            ]);
    }

    [Test]
    public async Task MalformedImplicitTargetDocumentationIsIgnoredWhenAnotherTargetIsValid()
    {
        const string testCode = """
            using System;

            /// <summary>Good contract.</summary>
            public interface IGood
            {
                /// <summary>Performs work.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                void Execute();
            }

            /// <summary>Bad contract.</summary>
            public interface IBad
            {
                /// <summary>Broken
                void Execute();
            }

            /// <summary>Implementation.</summary>
            public class Worker : IGood, IBad
            {
                /// <inheritdoc/>
                public void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        await VerifyCSenseAsync(
            testCode,
            expectDiagnostic: false,
            diagnosticOptions:
            [
                (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
                (CommentSenseDiagnosticIds.UnresolvedCrefId, ReportDiagnostic.Suppress)
            ]);
    }

    [Test]
    public async Task PartialMethodWithSingleDocumentedDeclarationResolvesInheritDoc()
    {
        const string testCode = """
            using System;

            /// <summary>Type.</summary>
            public partial class Worker
            {
                /// <summary>Source docs.</summary>
                /// <exception cref="InvalidOperationException">Thrown on failure.</exception>
                private void Source() { }

                /// <inheritdoc cref="Source"/>
                public partial void Execute();
            }

            public partial class Worker
            {
                public partial void Execute()
                {
                    throw new InvalidOperationException();
                }
            }
            """;

        var options = new Dictionary<string, string>
        {
            ["comment_sense.visibility_level"] = "Private"
        };

        await VerifyCSenseAsync(
            testCode,
            expectDiagnostic: false,
            configOptions: options,
            diagnosticOptions:
            [
                (CommentSenseDiagnosticIds.MissingDocumentationId, ReportDiagnostic.Suppress),
                (CommentSenseDiagnosticIds.InaccessibleCrefId, ReportDiagnostic.Suppress)
            ]);
    }
}
