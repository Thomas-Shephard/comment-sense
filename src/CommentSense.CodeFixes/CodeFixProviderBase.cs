using Microsoft.CodeAnalysis.CodeFixes;

namespace CommentSense.CodeFixes;

/// <summary>
/// Provides a base class for code fix providers in CommentSense.
/// </summary>
public abstract class CodeFixProviderBase : CodeFixProvider
{
    /// <inheritdoc />
    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
}
