using GameStatsGen.Models;

namespace GameStatsGen;

public static class Downsampling
{
    /// <summary>
    /// Buckets rows into <paramref name="targetPoints"/> equal-width time windows spanning
    /// [<paramref name="windowStart"/>, <paramref name="windowEnd"/>) and averages
    /// <paramref name="select"/> within each bucket. A bucket with no rows carries forward
    /// the previous bucket's value instead of leaving a gap.
    /// </summary>
    public static double[] BucketMean(
        IReadOnlyList<FrameRow> rows,
        double windowStart,
        double windowEnd,
        int targetPoints,
        Func<FrameRow, double> select)
    {
        var width = (windowEnd - windowStart) / targetPoints;
        var result = new double[targetPoints];

        for (var b = 0; b < targetPoints; b++)
        {
            var isLastBucket = b == targetPoints - 1;
            var lo = windowStart + b * width;
            var hi = isLastBucket ? windowEnd : lo + width;

            var sum = 0.0;
            var count = 0;
            foreach (var row in rows)
            {
                if (row.ElapsedS < lo) continue;
                if (isLastBucket ? row.ElapsedS > hi : row.ElapsedS >= hi) continue;
                sum += select(row);
                count++;
            }

            result[b] = count > 0 ? sum / count : (b > 0 ? result[b - 1] : 0.0);
        }

        return result;
    }
}
