namespace CommentSense.Core;

/// <summary>
/// Specifies the strictness mode for the ghost reference detector.
/// </summary>
public enum GhostReferenceMode
{
    /// <summary>
    /// The feature is disabled.
    /// </summary>
    Off,

    /// <summary>
    /// Only checks complex names (CamelCase, underscores, or digits) and ignores all-lowercase words entirely.
    /// </summary>
    Safe,

    /// <summary>
    /// Checks all parameters regardless of casing or length.
    /// </summary>
    Strict
}