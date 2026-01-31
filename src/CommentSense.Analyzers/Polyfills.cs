using System.Diagnostics.CodeAnalysis;

#pragma warning disable IDE0130
// ReSharper disable once CheckNamespace
namespace System.Diagnostics.CodeAnalysis
{
    [AttributeUsage(AttributeTargets.Parameter)]
    [ExcludeFromCodeCoverage]
    internal sealed class NotNullWhenAttribute(bool returnValue) : Attribute
    {
        public bool ReturnValue { get; } = returnValue;
    }
}

namespace System.Runtime.CompilerServices
{
    [ComponentModel.EditorBrowsable(ComponentModel.EditorBrowsableState.Never)]
    [ExcludeFromCodeCoverage]
    internal static class IsExternalInit { }
}

#pragma warning restore IDE0130
