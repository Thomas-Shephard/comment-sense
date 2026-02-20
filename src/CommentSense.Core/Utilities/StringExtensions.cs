namespace CommentSense.Core.Utilities;

internal static class StringExtensions
{
    public static double CalculateSimilarity(this string source, string target)
    {
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        int n = source.Length;
        int m = target.Length;

        // Early exit if the length difference itself makes it impossible to reach a reasonable similarity.
        // If one string is more than 2x the length of the other, similarity is at most 0.5.
        if (n > m * 2 || m > n * 2)
        {
            return (double)Math.Min(n, m) / Math.Max(n, m); // This is an upper bound on similarity for many algorithms
        }

        var distance = ComputeLevenshteinDistance(source.AsSpan(), target.AsSpan());
        return 1.0 - (double)distance / Math.Max(n, m);
    }

    private static int ComputeLevenshteinDistance(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
    {
        if (s.Length < t.Length)
        {
            var temp = s;
            s = t;
            t = temp;
        }

        int n = s.Length;
        int m = t.Length;

        const int maxStackLimit = 256;
        var rowSize = m + 1;
        Span<int> previousRow = rowSize <= maxStackLimit ? stackalloc int[rowSize] : new int[rowSize];
        Span<int> currentRow = rowSize <= maxStackLimit ? stackalloc int[rowSize] : new int[rowSize];

        for (var j = 0; j <= m; j++)
        {
            previousRow[j] = j;
        }

        for (var i = 0; i < n; i++)
        {
            currentRow[0] = i + 1;

            for (var j = 0; j < m; j++)
            {
                var cost = char.ToUpperInvariant(s[i]) == char.ToUpperInvariant(t[j]) ? 0 : 1;
                currentRow[j + 1] = Math.Min(
                    Math.Min(currentRow[j] + 1, previousRow[j + 1] + 1),
                    previousRow[j] + cost
                );
            }

            var tempRow = previousRow;
            previousRow = currentRow;
            currentRow = tempRow;
        }

        return previousRow[m];
    }
}
