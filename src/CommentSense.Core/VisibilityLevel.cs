namespace CommentSense.Core;

/// <summary>
/// Specifies the visibility threshold for members that should be analyzed.
/// </summary>
public enum VisibilityLevel
{
    /// <summary>
    /// Only public members are analyzed.
    /// </summary>
    Public,

    /// <summary>
    /// Public, protected, and protected internal members are analyzed.
    /// </summary>
    Protected,

    /// <summary>
    /// Public, protected, internal, and private protected members are analyzed.
    /// </summary>
    Internal,

    /// <summary>
    /// All members, including private ones, are analyzed.
    /// </summary>
    Private
}
