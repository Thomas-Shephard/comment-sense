using System.Collections.Immutable;
using System.Xml.Linq;
using CommentSense.Core;
using CommentSense.Core.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CommentSense.Analyzers.Logic;

internal static class InheritDocAnalyzer
{
    public static bool Analyze(SymbolAnalysisContext context, ISymbol symbol, XElement xml)
    {
        if (!DocumentationXmlExtensions.HasInheritDoc(xml))
            return false;

        if (!HasInvalidInheritDoc(context, symbol))
            return false;

        var location = symbol.Locations.GetPrimaryLocation();
        context.ReportDiagnostic(Diagnostic.Create(CommentSenseRules.InvalidInheritDocTargetRule, location, symbol.GetDisplayName()));
        return true;
    }

    private static bool HasInvalidInheritDoc(SymbolAnalysisContext context, ISymbol symbol)
    {
        ImmutableArray<ISymbol>? implicitTargets = null;

        foreach (var declaringReference in symbol.DeclaringSyntaxReferences)
        {
            var syntax = declaringReference.GetSyntax(context.CancellationToken);
            var docTrivia = DocumentationLocationExtensions.GetDocumentationCommentTrivia(syntax);
            if (docTrivia is null)
                continue;

            var semanticModel = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
            foreach (var node in docTrivia.DescendantNodes())
            {
                if (!TryGetInheritDocNode(node, out var crefAttribute))
                    continue;

                if (crefAttribute is not null)
                {
                    var symbolInfo = semanticModel.GetSymbolInfo(crefAttribute.Cref, context.CancellationToken);
                    if (symbolInfo.Symbol is null || !symbolInfo.Symbol.HasValidDocumentation())
                        return true;

                    continue;
                }

                implicitTargets ??= GetImplicitTargets(symbol);
                if (!HasDocumentedTarget(implicitTargets.Value))
                    return true;
            }
        }

        return false;
    }

    private static bool TryGetInheritDocNode(SyntaxNode node, out XmlCrefAttributeSyntax? crefAttribute)
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
        AddInterfaceTargets(symbol, builder);
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

    private static void AddInterfaceTargets(ISymbol symbol, ImmutableArray<ISymbol>.Builder builder)
    {
        var containingType = symbol.ContainingType;
        if (containingType is null || containingType.AllInterfaces.IsEmpty)
            return;

        if (containingType.TypeKind == TypeKind.Interface)
        {
            foreach (var implementedInterface in containingType.AllInterfaces)
            {
                foreach (var interfaceMember in implementedInterface.GetMembers(symbol.Name))
                {
                    if (MatchesInterfaceMember(symbol, interfaceMember))
                        builder.Add(interfaceMember);
                }
            }

            return;
        }

        foreach (var implementedInterface in containingType.AllInterfaces)
        {
            foreach (var interfaceMember in implementedInterface.GetMembers(symbol.Name))
            {
                if (interfaceMember.Kind != symbol.Kind)
                    continue;

                if (!SymbolEqualityComparer.Default.Equals(containingType.FindImplementationForInterfaceMember(interfaceMember), symbol))
                    continue;

                builder.Add(interfaceMember);
            }
        }
    }

    private static bool MatchesInterfaceMember(ISymbol symbol, ISymbol interfaceMember)
    {
        if (interfaceMember.Kind != symbol.Kind)
            return false;

        return symbol switch
        {
            IEventSymbol eventSymbol when interfaceMember is IEventSymbol baseEvent
                => SymbolEqualityComparer.Default.Equals(baseEvent.Type, eventSymbol.Type),
            IMethodSymbol methodSymbol when interfaceMember is IMethodSymbol baseMethod
                => MatchesMethod(methodSymbol, baseMethod),
            IPropertySymbol propertySymbol when interfaceMember is IPropertySymbol baseProperty
                => MatchesProperty(propertySymbol, baseProperty),
            _ => false
        };
    }

    private static bool MatchesMethod(IMethodSymbol method, IMethodSymbol baseMethod)
    {
        if (baseMethod.IsStatic != method.IsStatic)
            return false;

        if (baseMethod.ReturnsByRef != method.ReturnsByRef || baseMethod.RefKind != method.RefKind)
            return false;

        if (baseMethod.TypeParameters.Length != method.TypeParameters.Length)
            return false;

        if (baseMethod.Parameters.Length != method.Parameters.Length)
            return false;

        var substitutedBaseMethod = baseMethod;
        if (method.TypeParameters.Length > 0)
            substitutedBaseMethod = baseMethod.Construct([.. method.TypeParameters]);

        if (!SymbolEqualityComparer.Default.Equals(substitutedBaseMethod.ReturnType, method.ReturnType) &&
            !method.ReturnType.InheritsFromOrEquals(substitutedBaseMethod.ReturnType))
        {
            return false;
        }

        return !substitutedBaseMethod.Parameters
            .Where((parameter, index) => parameter.RefKind != method.Parameters[index].RefKind ||
                                         !SymbolEqualityComparer.Default.Equals(parameter.Type, method.Parameters[index].Type))
            .Any();
    }

    private static bool MatchesProperty(IPropertySymbol property, IPropertySymbol baseProperty)
    {
        if (baseProperty.IsStatic != property.IsStatic)
            return false;

        if (baseProperty.ReturnsByRef != property.ReturnsByRef || baseProperty.RefKind != property.RefKind)
            return false;

        if (!SymbolEqualityComparer.Default.Equals(baseProperty.Type, property.Type) &&
            !property.Type.InheritsFromOrEquals(baseProperty.Type))
        {
            return false;
        }

        if (baseProperty.Parameters.Length != property.Parameters.Length)
            return false;

        return !baseProperty.Parameters
            .Where((parameter, index) => !SymbolEqualityComparer.Default.Equals(parameter.Type, property.Parameters[index].Type))
            .Any();
    }
}
