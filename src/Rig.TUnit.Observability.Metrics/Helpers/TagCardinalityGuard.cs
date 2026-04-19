namespace Rig.TUnit.Observability.Metrics.Helpers;

/// <summary>
/// Pure cardinality guard — throws when a tag's distinct-value count exceeds the
/// configured maximum. Surfaces accidental high-cardinality instrumentation that
/// would otherwise explode a TSDB cardinality budget at runtime.
/// </summary>
public static class TagCardinalityGuard
{
    /// <summary>
    /// Returns true when <paramref name="distinctCount"/> is within the allowed
    /// <paramref name="maxCardinality"/>; throws <see cref="CardinalityException"/> otherwise.
    /// </summary>
    public static bool EnsureWithinBudget(string tagName, int distinctCount, int maxCardinality)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tagName);
        if (distinctCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(distinctCount), distinctCount, "Distinct count must be non-negative.");
        }
        if (maxCardinality < 1)
        {
            throw new ArgumentOutOfRangeException(nameof(maxCardinality), maxCardinality, "Maximum cardinality must be >= 1.");
        }

        if (distinctCount > maxCardinality)
        {
            throw new CardinalityException(
                $"Tag '{tagName}' has {distinctCount} distinct values, exceeds budget of {maxCardinality}.");
        }

        return true;
    }
}

public sealed class CardinalityException : Exception
{
    public CardinalityException(string message) : base(message) { }
}
