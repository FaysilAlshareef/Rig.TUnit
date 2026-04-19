namespace Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

/// <summary>
/// Pure helper for asserting partition-key distribution health in Cosmos.
/// A hot-partition anti-pattern forms when a single partition key value owns
/// disproportionate traffic. This checker computes the distribution entropy
/// (normalised Shannon entropy, range 0..1) and flags hot keys.
/// </summary>
public static class PartitionKeyDistributionChecker
{
    /// <summary>
    /// Computes the share (fraction 0..1) of the most-loaded partition key.
    /// 1.0 means all documents land on one key (pure hot partition).
    /// </summary>
    public static double MaxShare(IReadOnlyDictionary<string, int> countsByKey)
    {
        ArgumentNullException.ThrowIfNull(countsByKey);
        if (countsByKey.Count == 0) return 0.0;
        var total = countsByKey.Values.Sum();
        if (total == 0) return 0.0;
        var max = countsByKey.Values.Max();
        return (double)max / total;
    }

    /// <summary>
    /// Normalised Shannon entropy of the distribution. 1.0 = perfectly even,
    /// 0.0 = all traffic on one key. Good signal for distribution health.
    /// </summary>
    public static double NormalisedEntropy(IReadOnlyDictionary<string, int> countsByKey)
    {
        ArgumentNullException.ThrowIfNull(countsByKey);
        if (countsByKey.Count <= 1) return countsByKey.Count == 0 ? 0.0 : 1.0;

        var total = (double)countsByKey.Values.Sum();
        if (total == 0) return 0.0;

        double entropy = 0.0;
        foreach (var count in countsByKey.Values)
        {
            if (count == 0) continue;
            var p = count / total;
            entropy -= p * Math.Log2(p);
        }

        var max = Math.Log2(countsByKey.Count);
        return max == 0 ? 1.0 : entropy / max;
    }

    /// <summary>
    /// Returns true when <see cref="MaxShare"/> is within <paramref name="threshold"/>
    /// (e.g. 0.3 means no single key owns more than 30% of traffic).
    /// </summary>
    public static bool IsHealthy(IReadOnlyDictionary<string, int> countsByKey, double threshold)
    {
        ArgumentNullException.ThrowIfNull(countsByKey);
        if (threshold is <= 0.0 or > 1.0)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be in (0, 1].");
        }
        return MaxShare(countsByKey) <= threshold;
    }
}
