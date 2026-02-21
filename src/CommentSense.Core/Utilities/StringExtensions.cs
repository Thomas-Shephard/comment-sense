using System.Buffers;

namespace CommentSense.Core.Utilities;

internal static class StringExtensions
{
    public static double CalculateSimilarity(this string source, string target)
    {
        return CalculateSimilarity(source.AsSpan(), target.AsSpan());
    }

    public static double CalculateSimilarity(this ReadOnlySpan<char> source, string target)
    {
        return CalculateSimilarity(source, target.AsSpan());
    }

    public static double CalculateSimilarity(this ReadOnlySpan<char> source, ReadOnlySpan<char> target)
    {
        if (source.Equals(target, StringComparison.OrdinalIgnoreCase))
            return 1.0;

        int n = source.Length;
        int m = target.Length;

        if (n == 0 || m == 0)
            return 0.0;

        // Cap length to avoid excessive memory pressure for similarity checks
        if (n > 2048 || m > 2048)
            return 0.0;

        var distance = ComputeLevenshteinDistance(source, target);
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

        int[]? previousRowArray = null;
        int[]? currentRowArray = null;
        char[]? tUpperArray = null;

        try
        {
            bool useStack = rowSize <= maxStackLimit;
            Span<int> previousRow = useStack
                ? stackalloc int[rowSize]
                : (previousRowArray = ArrayPool<int>.Shared.Rent(rowSize)).AsSpan(0, rowSize);

            Span<int> currentRow = useStack
                ? stackalloc int[rowSize]
                : (currentRowArray = ArrayPool<int>.Shared.Rent(rowSize)).AsSpan(0, rowSize);

            Span<char> tUpper = m <= maxStackLimit
                ? stackalloc char[m]
                : (tUpperArray = ArrayPool<char>.Shared.Rent(m)).AsSpan(0, m);

            // Pre-compute upper-case version of the shorter string to avoid redundant calls in the inner loop.
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
        finally
        {
            if (previousRowArray != null)
                ArrayPool<int>.Shared.Return(previousRowArray, clearArray: false);
            if (currentRowArray != null)
                ArrayPool<int>.Shared.Return(currentRowArray, clearArray: false);
            if (tUpperArray != null)
                ArrayPool<char>.Shared.Return(tUpperArray, clearArray: false);
        }
    }
}
