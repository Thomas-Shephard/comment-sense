namespace CommentSense.CodeFixes;

internal static class Guard
{
    internal static T AgainstNull<T>(T? value, string? message = null)
        where T : class
    {
        if (value is null)
            throw new InvalidOperationException(message ?? "Unexpected null value.");

        return value;
    }

    internal static TResult WhenNotNull<T, TResult>(T? value, Func<T, TResult> whenValue, TResult whenNull)
        where T : class
    {
        if (value is null)
            return whenNull;

        return whenValue(value);
    }
}
