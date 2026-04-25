namespace CommentSense.Analyzers;

internal static class AnalyzerGuard
{
    internal static T AgainstNull<T>(T? value, string? message = null)
        where T : class
    {
        if (value is null)
            throw new InvalidOperationException(message ?? "Unexpected null value.");

        return value;
    }
}
