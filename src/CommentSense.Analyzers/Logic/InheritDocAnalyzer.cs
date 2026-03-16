using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class InheritDocAnalyzer
{
    internal static ImmutableArray<ISymbol> GetImplicitTargetsForInheritDoc(ISymbol symbol) => GetImplicitTargets(symbol);

    public static bool Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml)
    {
        if (!HasTopLevelInheritDoc(xml))
            return false;

        if (!HasInvalidInheritDoc(context, symbol))
            return false;

        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidInheritDocTargetRule, location, symbol.GetDisplayName()));
        return true;
    }

    private static bool HasTopLevelInheritDoc(XElement xml)
    {
        return DocumentationXmlExtensions.GetTargetElements(xml, DocumentationTags.InheritDoc, recursive: false).Any();
    }

    private static bool HasInvalidInheritDoc(SymbolAnalysisContext context, ISymbol symbol)
    {
        ImmutableArray<ISymbol>? implicitTargets = null;

        foreach (var declaringReference in symbol.DeclaringSyntaxReferences)
        {
            if (!TryGetDocumentationContext(context, declaringReference, out var docTrivia, out var semanticModel))
                continue;

            if (HasInvalidInheritDocInTrivia(context, symbol, docTrivia, semanticModel, ref implicitTargets))
                return true;
        }

        return false;
    }

    private static bool TryGetDocumentationContext(
        SymbolAnalysisContext context,
        SyntaxReference declaringReference,
        [NotNullWhen(true)] out DocumentationCommentTriviaSyntax? docTrivia,
        out SemanticModel semanticModel)
    {
        var syntax = declaringReference.GetSyntax(context.CancellationToken);
        semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
        docTrivia = DocumentationLocationExtensions.GetDocumentationCommentTrivia(syntax);
        return docTrivia is not null;
    }

    private static bool HasInvalidInheritDocInTrivia(
        SymbolAnalysisContext context,
        ISymbol symbol,
        DocumentationCommentTriviaSyntax docTrivia,
        SemanticModel semanticModel,
        ref ImmutableArray<ISymbol>? implicitTargets)
    {
        foreach (var node in docTrivia.Content)
        {
            if (!TryGetInheritDocNode(node, out var crefAttribute))
                continue;

            if (crefAttribute is not null)
            {
                if (IsInvalidCrefTarget(context, semanticModel, crefAttribute))
                    return true;

                continue;
            }

            if (IsInvalidImplicitTarget(symbol, ref implicitTargets))
                return true;
        }

        return false;
    }

    private static bool IsInvalidCrefTarget(
        SymbolAnalysisContext context,
        SemanticModel semanticModel,
        XmlCrefAttributeSyntax crefAttribute)
    {
        var symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref, context.CancellationToken);
        return symbolInfo.Symbol is null || !symbolInfo.Symbol.HasValidDocumentation();
    }

    private static bool IsInvalidImplicitTarget(ISymbol symbol, ref ImmutableArray<ISymbol>? implicitTargets)
    {
        implicitTargets ??= GetImplicitTargets(symbol);
        return !HasDocumentedTarget(implicitTargets.Value);
    }

    internal static bool TryGetInheritDocNode(SyntaxNode node, out XmlCrefAttributeSyntax? crefAttribute)
    {
        switch (node)
        {
            case XmlEmptyElementSyntax { Name.LocalName.ValueText: DocumentationTags.InheritDoc } emptyElement:
                crefAttribute = GetCrefAttribute(emptyElement.Attributes);
                return true;
            case XmlElementSyntax { StartTag.Name.LocalName.ValueText: DocumentationTags.InheritDoc } element:
                crefAttribute = GetCrefAttribute(element.StartTag.Attributes);
                return true;
            default:
                crefAttribute = null;
                return false;
        }
    }

    private static XmlCrefAttributeSyntax? GetCrefAttribute(SyntaxList<XmlAttributeSyntax> attributes)
    {
        foreach (var attribute in attributes)
        {
            if (attribute is XmlCrefAttributeSyntax { Name.LocalName.ValueText: DocumentationAttributes.Cref } crefAttribute)
                return crefAttribute;
        }

        return null;
    }

    private static bool HasDocumentedTarget(ImmutableArray<ISymbol> targets)
    {
        foreach (var target in targets)
        {
            if (target.HasValidDocumentation())
                return true;
        }

        return false;
    }

    private static ImmutableArray<ISymbol> GetImplicitTargets(ISymbol symbol)
    {
        if (symbol is INamedTypeSymbol typeSymbol)
            return GetTypeTargets(typeSymbol);

        var builder = ImmutableArray.CreateBuilder<ISymbol>();
        AddOverrideTargets(symbol, builder);

        if (symbol.ContainingType is { AllInterfaces.IsEmpty: false } containingType)
            AddInterfaceTargets(symbol, containingType, builder);

        return builder.ToImmutable();
    }

    private static ImmutableArray<ISymbol> GetTypeTargets(INamedTypeSymbol symbol)
    {
        var builder = ImmutableArray.CreateBuilder<ISymbol>();

        for (var current = symbol.BaseType; current is not null; current = current.BaseType)
        {
            if (current.SpecialType is SpecialType.System_Object or SpecialType.System_ValueType or SpecialType.System_Enum or SpecialType.System_MulticastDelegate)
                break;

            builder.Add(current);
        }

        foreach (var implementedInterface in symbol.AllInterfaces)
        {
            builder.Add(implementedInterface);
        }

        return builder.ToImmutable();
    }

    private static void AddOverrideTargets(ISymbol symbol, ImmutableArray<ISymbol>.Builder builder)
    {
        switch (symbol)
        {
            case IMethodSymbol methodSymbol:
                for (var current = methodSymbol.OverriddenMethod; current is not null; current = current.OverriddenMethod)
                {
                    builder.Add(current);
                }
                break;
            case IPropertySymbol propertySymbol:
                for (var current = propertySymbol.OverriddenProperty; current is not null; current = current.OverriddenProperty)
                {
                    builder.Add(current);
                }
                break;
            case IEventSymbol eventSymbol:
                for (var current = eventSymbol.OverriddenEvent; current is not null; current = current.OverriddenEvent)
                {
                    builder.Add(current);
                }
                break;
        }
    }

    private static void AddInterfaceTargets(ISymbol symbol, INamedTypeSymbol containingType, ImmutableArray<ISymbol>.Builder builder)
    {
        if (containingType.TypeKind == TypeKind.Interface)
        {
            AddInterfaceTargetsForInterfaceSymbol(symbol, containingType, builder);
            return;
        }

        AddInterfaceTargetsForClassSymbol(symbol, containingType, builder);
    }

    private static void AddInterfaceTargetsForInterfaceSymbol(
        ISymbol symbol,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol>.Builder builder)
    {
        foreach (var implementedInterface in containingType.AllInterfaces)
        {
            foreach (var interfaceMember in implementedInterface.GetMembers(symbol.Name))
            {
                if (symbol.MatchesInterfaceMemberSignature(interfaceMember))
                    builder.Add(interfaceMember);
            }
        }
    }

    private static void AddInterfaceTargetsForClassSymbol(
        ISymbol symbol,
        INamedTypeSymbol containingType,
        ImmutableArray<ISymbol>.Builder builder)
    {
        foreach (var implementedInterface in containingType.AllInterfaces)
        {
            foreach (var interfaceMember in implementedInterface.GetMembers(symbol.Name))
            {
                if (IsImplicitInterfaceImplementationMatch(symbol, containingType, interfaceMember))
                    builder.Add(interfaceMember);
            }
        }
    }

    private static bool IsImplicitInterfaceImplementationMatch(ISymbol symbol, INamedTypeSymbol containingType, ISymbol interfaceMember)
    {
        if (interfaceMember.Kind != symbol.Kind)
            return false;

        return SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMember), symbol);
    }
}
