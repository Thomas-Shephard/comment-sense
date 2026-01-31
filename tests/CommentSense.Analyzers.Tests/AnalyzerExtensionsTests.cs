using System.Collections.Immutable;
using CommentSense.Core;
using CommentSense.TestHelpers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using NUnit.Framework;

namespace CommentSense.Analyzers.Tests;

public class AnalyzerExtensionsTests
{
    private static readonly List<MetadataReference> CachedReferences = AppDomain.CurrentDomain.GetAssemblies()
        .Where(a => !a.IsDynamic && !string.IsNullOrWhiteSpace(a.Location))
        .Select<System.Reflection.Assembly, MetadataReference>(a => MetadataReference.CreateFromFile(a.Location))
        .ToList();

    [Test]
    public void GetPrimaryLocationReturnsLocationNoneForEmptyArray()
    {
        var locations = ImmutableArray<Location>.Empty;
        var result = locations.GetPrimaryLocation();

        Assert.That(result, Is.EqualTo(Location.None));
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstLocation()
    {
        var location = Location.Create("test.cs", default, default);
        var locations = ImmutableArray.Create(location);
        var result = locations.GetPrimaryLocation();

        Assert.That(result, Is.EqualTo(location));
    }

    [Test]
    public void GetPrimaryLocationReturnsFirstOfMultipleLocations()
    {
        var loc1 = Location.Create("f1.cs", default, default);
        var loc2 = Location.Create("f2.cs", default, default);
        var locations = ImmutableArray.Create(loc1, loc2);

        Assert.That(locations.GetPrimaryLocation(), Is.EqualTo(loc1));
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForImplicitlyDeclared()
    {
        const string source = "public class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ctor = symbol.GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.Constructor);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ctor.IsImplicitlyDeclared, Is.True);
            Assert.That(ctor.IsEligibleForAnalysis(), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForPropertyAccessors()
    {
        const string source = "public class C { public int P { get; set; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var prop = (IPropertySymbol)symbol.GetMembers().First(m => m.Name == "P");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(prop.GetMethod?.IsEligibleForAnalysis(), Is.False);
            Assert.That(prop.SetMethod?.IsEligibleForAnalysis(), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForEventAccessors()
    {
        const string source = "using System; public class C { public event EventHandler E; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ev = (IEventSymbol)symbol.GetMembers().First(m => m.Name == "E");

        using (Assert.EnterMultipleScope())
        {
            Assert.That(ev.AddMethod?.IsEligibleForAnalysis(), Is.False);
            Assert.That(ev.RemoveMethod?.IsEligibleForAnalysis(), Is.False);
        }
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForInaccessibleSymbol()
    {
        const string source = "internal class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicSymbol()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicProperty()
    {
        const string source = "public class C { public int P { get; set; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var prop = symbol.GetMembers().First(m => m.Name == "P");

        Assert.That(prop.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicField()
    {
        const string source = "public class C { public int f; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var field = symbol.GetMembers().First(m => m.Name == "f");

        Assert.That(field.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForCompilerServiceNamespace()
    {
        const string source = "namespace System.Runtime.CompilerServices { public class IsExternalInit {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "IsExternalInit");

        Assert.That(symbol.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForSymbolInGlobalNamespace()
    {
        const string source = "public class GlobalClass {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "GlobalClass");

        Assert.That(symbol.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForSymbolInOtherNamespace()
    {
        const string source = "namespace Other { public class C {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");

        Assert.That(symbol.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForPrimaryConstructor()
    {
        const string source = "public class C(int p1) {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var primaryCtor = symbol.GetPrimaryConstructor();

        Assert.That(primaryCtor?.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForRecordPositionalProperty()
    {
        const string source = "namespace System.Runtime.CompilerServices { public class IsExternalInit {} } public record R(int p1);";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "R");
        var prop = symbol.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "p1");

        Assert.That(prop.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForRecordManualProperty()
    {
        const string source = "namespace System.Runtime.CompilerServices { public class IsExternalInit {} } public record R(int p1) { public int P2 { get; init; } }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "R");
        var prop = symbol.GetMembers().OfType<IPropertySymbol>().First(p => p.Name == "P2");

        Assert.That(prop.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForExplicitConstructor()
    {
        const string source = "public class C { public C() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var ctor = symbol.InstanceConstructors.First();

        Assert.That(ctor.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForRecordField()
    {
        const string source = "namespace System.Runtime.CompilerServices { public class IsExternalInit {} } public record R(int p1) { public int f1; }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "R");
        var field = symbol.GetMembers().OfType<IFieldSymbol>().First(f => f.Name == "f1");

        Assert.That(field.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForTypeParameter()
    {
        const string source = "public class C<T> {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var typeParam = symbol.TypeParameters.First();

        Assert.That(typeParam.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForAssembly()
    {
        var compilation = CSharpCompilation.Create("TestAssembly");
        var symbol = compilation.Assembly;

        Assert.That(symbol.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForPublicMethod()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForInternalWhenIncluded()
    {
        const string source = "internal class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        Assert.That(method.IsEligibleForAnalysis(VisibilityLevel.Internal), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForUndefinedVisibilityLevel()
    {
        const string source = "public class C { public void M() {} }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var method = symbol.GetMembers().First(m => m.Name == "M");

        // Cast an undefined value to VisibilityLevel to trigger the fallback case
        Assert.That(method.IsEligibleForAnalysis((VisibilityLevel)999), Is.False);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForOperator()
    {
        const string source = "public class C { public static bool operator ==(C a, C b) => true; public static bool operator !=(C a, C b) => false; }";
        var symbol = ((INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C")).GetMembers("op_Equality").First();
        Assert.That(symbol.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsTrueForConversion()
    {
        const string source = "public class C { public static implicit operator int(C c) => 0; }";
        var symbol = ((INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C")).GetMembers("op_Implicit").First();
        Assert.That(symbol.IsEligibleForAnalysis(), Is.True);
    }

    [Test]
    public void IsEligibleForAnalysisReturnsFalseForDestructor()
    {
        const string source = "public class C { ~C() {} }";
        var symbol = ((INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C")).GetMembers().OfType<IMethodSymbol>().First(m => m.MethodKind == MethodKind.Destructor);
        Assert.That(symbol.IsEligibleForAnalysis(), Is.False);
    }

    [Test]
    public void GetPrimaryConstructorReturnsNullForStaticClass()
    {
        const string source = "public static class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var result = symbol.GetPrimaryConstructor();

        Assert.That(result, Is.Null);
    }

    [Test]
    public void IsPrimaryConstructorReturnsFalseForMetadataConstructor()
    {
        var compilation = CSharpCompilation.Create("TestAssembly",
            options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
            .AddReferences(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
        var objectType = compilation.GetSpecialType(SpecialType.System_Object);
        var ctor = objectType.InstanceConstructors.First();

        Assert.That(ctor.IsPrimaryConstructor(), Is.False);
    }

    [Test]
    public void IsPrimaryConstructorReturnsTrueForPartialClassPrimaryConstructor()
    {
        const string source = "partial class C(int p1); partial class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var primaryCtor = symbol.GetPrimaryConstructor();

        Assert.That(primaryCtor, Is.Not.Null);
        Assert.That(primaryCtor.IsPrimaryConstructor(), Is.True);
    }

    [Test]
    public void IsInExceptionTagReturnsTrueForSelfClosingExceptionTag()
    {
        var cref = SyntaxFactory.XmlCrefAttribute(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("ArgumentNullException")));
        var element = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("exception"), SyntaxFactory.List<XmlAttributeSyntax>([cref]));

        var attachedCref = (XmlCrefAttributeSyntax)element.Attributes.First();

        Assert.That(attachedCref.IsInExceptionTag(), Is.True);
    }

    [Test]
    public void IsInExceptionTagReturnsTrueForStartTagExceptionTag()
    {
        var cref = SyntaxFactory.XmlCrefAttribute(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("ArgumentNullException")));
        var startTag = SyntaxFactory.XmlElementStartTag(SyntaxFactory.XmlName("exception"), SyntaxFactory.List<XmlAttributeSyntax>([cref]));

        var attachedCref = (XmlCrefAttributeSyntax)startTag.Attributes.First();

        Assert.That(attachedCref.IsInExceptionTag(), Is.True);
    }

    [Test]
    public void IsInExceptionTagReturnsFalseForOtherTag()
    {
        var cref = SyntaxFactory.XmlCrefAttribute(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("String")));
        var element = SyntaxFactory.XmlEmptyElement(SyntaxFactory.XmlName("see"), SyntaxFactory.List<XmlAttributeSyntax>([cref]));

        var attachedCref = (XmlCrefAttributeSyntax)element.Attributes.First();

        Assert.That(attachedCref.IsInExceptionTag(), Is.False);
    }

    [Test]
    public void IsInExceptionTagReturnsFalseForDetachedAttribute()
    {
        var cref = SyntaxFactory.XmlCrefAttribute(SyntaxFactory.TypeCref(SyntaxFactory.ParseTypeName("ArgumentNullException")));

        using (Assert.EnterMultipleScope())
        {
            Assert.That(cref.Parent, Is.Null);
            Assert.That(cref.IsInExceptionTag(), Is.False);
        }
    }

    [Test]
    public void IsDocumentationModeNoneReturnsTrueForNullTree()
    {
        SyntaxTree? tree = null;
        Assert.That(tree.IsDocumentationModeNone(), Is.True);
    }

    [Test]
    public void IsDocumentationModeNoneReturnsTrueForNoneMode()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.None);
        var tree = CSharpSyntaxTree.ParseText("", options);
        Assert.That(tree.IsDocumentationModeNone(), Is.True);
    }

    [Test]
    public void IsDocumentationModeNoneReturnsFalseForParseMode()
    {
        var options = new CSharpParseOptions(documentationMode: DocumentationMode.Parse);
        var tree = CSharpSyntaxTree.ParseText("", options);
        Assert.That(tree.IsDocumentationModeNone(), Is.False);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForSelf()
    {
        const string source = "public class C {}";
        var symbol = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        Assert.That(symbol.InheritsFromOrEquals(symbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForBaseClass()
    {
        const string source = "public class B {} public class C : B {}";
        var symbol = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var baseSymbol = ((INamedTypeSymbol)symbol).BaseType ?? throw new InvalidOperationException();
        Assert.That(symbol.InheritsFromOrEquals(baseSymbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForGrandParentClass()
    {
        const string source = "public class A {} public class B : A {} public class C : B {}";
        var symbol = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var baseSymbol = ((INamedTypeSymbol)symbol).BaseType?.BaseType ?? throw new InvalidOperationException();
        Assert.That(symbol.InheritsFromOrEquals(baseSymbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForInterface()
    {
        const string source = "public interface I {} public class C : I {}";
        var symbol = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        var interfaceSymbol = ((INamedTypeSymbol)symbol).Interfaces.First();
        Assert.That(symbol.InheritsFromOrEquals(interfaceSymbol), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsTrueForInterfaceInheritingInterface()
    {
        const string source = "public interface I_BaseI {} public interface I_DerI : I_BaseI {}";
        var (symbol, compilation) = GetSymbolsFromSource(source, "I_DerI");
        var i1 = compilation.SyntaxTrees.First().GetRoot().DescendantNodes().OfType<InterfaceDeclarationSyntax>().First(i => i.Identifier.ValueText == "I_BaseI");
        var symbol1 = compilation.GetSemanticModel(compilation.SyntaxTrees.First()).GetDeclaredSymbol(i1) ?? throw new InvalidOperationException();
        Assert.That(((ITypeSymbol)symbol).InheritsFromOrEquals(symbol1), Is.True);
    }

    [Test]
    public void InheritsFromOrEqualsReturnsFalseForUnrelated()
    {
        const string source = "public class A {} public class B {}";
        var symbolA = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "A");
        var symbolB = (ITypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "B");
        Assert.That(symbolA.InheritsFromOrEquals(symbolB), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForObjectInheritanceOnly()
    {
        const string source = "public class C {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForStructValueTypeInheritance()
    {
        const string source = "public struct S {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "S");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForEnumInheritance()
    {
        const string source = "public enum E { A }";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "E");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForDelegateInheritance()
    {
        const string source = "public delegate void D();";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "D");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsTrueForClassInheritance()
    {
        const string source = "public class B {} public class C : B {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForInterfaceImplementation()
    {
        const string source = "public interface I {} public class C : I {}";
        var symbol = (INamedTypeSymbol)RoslynTestUtils.GetSymbolFromSource(source, "C");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForExplicitMethodImplementation()
    {
        const string source = "public interface I_Exp { void M(); } public class C_Exp : I_Exp { void I_Exp.M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForExplicitPropertyImplementation()
    {
        const string source = "public interface I_PropExp { int P { get; } } public class C_PropExp : I_PropExp { int I_PropExp.P => 0; }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForExplicitEventImplementation()
    {
        const string source = "using System; public interface I { event EventHandler E; } public class C : I { event EventHandler I.E { add {} remove {} } }";
        var (symbol, _) = GetSymbolsFromSource(source, "E");
        using (Assert.EnterMultipleScope())
        {
            Assert.That(symbol, Is.InstanceOf<IEventSymbol>());
            Assert.That(((IEventSymbol)symbol).ExplicitInterfaceImplementations, Is.Not.Empty);
        }
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForOverride()
    {
        const string source = "public class B_Ovr { public virtual void M() {} } public class C_Ovr : B_Ovr { public override void M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForImplicitInterfaceMethodImplementation()
    {
        const string source = "public interface I_Imp { void M(); } public class C_Imp : I_Imp { public void M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForImplicitInterfacePropertyImplementation()
    {
        const string source = "public interface I_Prop { int P { get; } } public class C_Prop : I_Prop { public int P { get; } }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForGenericMethodImplementation()
    {
        const string source = "public interface I_Gen { void M<T>(T t); } public class C_Gen : I_Gen { public void M<T>(T t) {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForCovariantReturnType()
    {
        const string source = "public class B_Cov { public virtual object M() => null; } public class C_Cov : B_Cov { public override string M() => \"\"; }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForInterfaceMemberInheritingBaseInterfaceMember()
    {
        const string source = "public interface I_Base { void M(); } public interface I_Der : I_Base { void M(); }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsTrueForCovariantPropertyReturnType()
    {
        const string source = "public class B_PropCov { public virtual object P => null; } public class C_PropCov : B_PropCov { public override string P => \"\"; }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.True);
    }

    [Test]
    public void IsInheritingReturnsFalseForNonPublicImplicitImplementation()
    {
        const string source = "public interface I_NonPub { void M(); } public class C_NonPub : I_NonPub { void I_NonPub.M() {} internal void M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForUnrelatedMemberWithSameName()
    {
        const string source = "public interface I_Unrelated { void M(); } public class C_Unrelated { public void M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsInheritingReturnsFalseForNullContainingType()
    {
        var compilation = CSharpCompilation.Create("TestAssembly");
        var symbol = compilation.GlobalNamespace;
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesInterfaceMemberReturnsFalseForDifferentKind()
    {
        const string source = "public interface I1 { void M(); } public interface I2 : I1 { int M { get; } }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesMethodReturnsFalseForStaticMismatch()
    {
        const string source = "public interface I1 { void M(); } public interface I2 : I1 { static void M() {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesMethodReturnsFalseForRefReturnMismatch()
    {
        const string source = "public interface I1 { int M(); } public interface I2 : I1 { ref int M(); }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesMethodReturnsFalseForTypeParameterCountMismatch()
    {
        const string source = "public interface I1 { void M(); } public interface I2 : I1 { void M<T>(); }";
        var (symbol, _) = GetSymbolsFromSource(source, "M");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesPropertyReturnsFalseForStaticMismatch()
    {
        const string source = "public interface I1 { int P { get; } } public interface I2 : I1 { static int P => 0; }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesPropertyReturnsFalseForRefReturnMismatch()
    {
        const string source = "public interface I1 { int P { get; } } public interface I2 : I1 { ref int P { get; } }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesPropertyReturnsFalseForTypeMismatch()
    {
        const string source = "public interface I1 { int P { get; } } public interface I2 : I1 { string P { get; } }";
        var (symbol, _) = GetSymbolsFromSource(source, "P");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void MatchesPropertyReturnsFalseForParameterCountMismatch()
    {
        const string source = "public interface I1 { int this[int i] { get; } } public interface I2 : I1 { int this[int i, int j] { get; } }";
        var (symbol, _) = GetSymbolsFromSource(source, "this[]");
        Assert.That(symbol.IsInheriting(), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsTrueForTask()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") ?? throw new InvalidOperationException();
        Assert.That(taskType.IsTaskType(), Is.True);
    }

    [Test]
    public void IsTaskTypeReturnsTrueForValueTask()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.ValueTask") ?? throw new InvalidOperationException();
        Assert.That(taskType.IsTaskType(), Is.True);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForGenericTaskWhenIsGenericIsFalse()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1") ?? throw new InvalidOperationException();
        Assert.That(taskType.IsTaskType(isGeneric: false), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsTrueForGenericTaskWhenIsGenericIsTrue()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1") ?? throw new InvalidOperationException();
        Assert.That(taskType.IsTaskType(isGeneric: true), Is.True);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForNonTaskType()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var stringType = compilation.GetSpecialType(SpecialType.System_String);
        Assert.That(stringType.IsTaskType(), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForCustomTaskType()
    {
        const string source = "namespace Custom { public class Task {} }";
        var (symbol, _) = GetSymbolsFromSource(source, "Task");
        Assert.That(((ITypeSymbol)symbol).IsTaskType(), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForNullNamespace()
    {
        const string source = "public class Task {}";
        var (symbol, _) = GetSymbolsFromSource(source, "Task");
        Assert.That(((ITypeSymbol)symbol).IsTaskType(), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForNonNamedType()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var arrayType = compilation.CreateArrayTypeSymbol(compilation.GetSpecialType(SpecialType.System_Int32));
        Assert.That(arrayType.IsTaskType(), Is.False);
    }

    [Test]
    public void IsTaskTypeReturnsFalseForDifferentArity()
    {
        var compilation = CSharpCompilation.Create("Test", references: CachedReferences);
        var taskType = compilation.GetTypeByMetadataName("System.Threading.Tasks.Task") ?? throw new InvalidOperationException();
        Assert.That(taskType.IsTaskType(isGeneric: true), Is.False);
    }

    private static (ISymbol symbol, Compilation compilation) GetSymbolsFromSource(string source, string symbolName)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var compilation = CSharpCompilation.Create("TestAssembly", [tree], CachedReferences);
        var semanticModel = compilation.GetSemanticModel(tree);
        var declaration = tree.GetRoot().DescendantNodes()
            .Last(n => n is MemberDeclarationSyntax m && (m is MethodDeclarationSyntax md && md.Identifier.ValueText == symbolName ||
                                                         m is PropertyDeclarationSyntax pd && pd.Identifier.ValueText == symbolName ||
                                                         m is IndexerDeclarationSyntax id && (symbolName == "this[]" || id.ThisKeyword.ValueText == "this") ||
                                                         m is EventDeclarationSyntax ed && ed.Identifier.ValueText == symbolName ||
                                                         m is BaseTypeDeclarationSyntax td && td.Identifier.ValueText == symbolName));
        var declaredSymbol = semanticModel.GetDeclaredSymbol(declaration) ?? throw new InvalidOperationException();
        return (declaredSymbol, compilation);
    }
}
