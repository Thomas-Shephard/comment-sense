#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis;
#pragma warning restore IDE0130

[AttributeUsage(AttributeTargets.Parameter)]
[ExcludeFromCodeCoverage]
internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
{
    public bool ReturnValue { get; } = returnValue;
}
