namespace CommentSense.Core.Utilities;

internal static class StringExtensions
{
    public static double CalculateSimilarity(this string source, string target)
    {
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        int n = source.Length;
        int m = target.Length;

        if (n == 0 || m == 0)
            return 0.0;

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

        // Pre-compute upper-case version of the shorter string to avoid redundant calls in the inner loop.
        Span<char> tUpper = m <= maxStackLimit ? stackalloc char[m] : new char[m];
        for (var j = 0; j < m; j++)
        {
            tUpper[j] = char.ToUpperInvariant(t[j]);
        }

        for (var j = 0; j <= m; j++)
        {
            previousRow[j] = j;
        }

        for (var i = 0; i < n; i++)
        {
            var sChar = char.ToUpperInvariant(s[i]);
            currentRow[0] = i + 1;

            for (var j = 0; j < m; j++)
            {
                var cost = sChar == tUpper[j] ? 0 : 1;
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
