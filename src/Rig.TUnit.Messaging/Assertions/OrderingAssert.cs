using Rig.TUnit.Messaging.Helpers;

namespace Rig.TUnit.Messaging.Assertions;

public sealed class OrderingAssert
{
    private OrderingAssert() { }

    /// <summary>
    /// Asserts that messages with the same partition key appear in monotonically
    /// non-decreasing order by <paramref name="sequenceExtractor"/>.
    /// </summary>
    public static void PerKeyMonotonic<T>(
        ListenerBase<T> listener,
        Func<T, string> keyExtractor,
        Func<T, long> sequenceExtractor) where T : class
    {
        ArgumentNullException.ThrowIfNull(listener);
        ArgumentNullException.ThrowIfNull(keyExtractor);
        ArgumentNullException.ThrowIfNull(sequenceExtractor);

        var byKey = listener.Captured
            .GroupBy(m => keyExtractor(m.Message), StringComparer.Ordinal)
            .ToArray();

        foreach (var group in byKey)
        {
            long last = long.MinValue;
            foreach (var m in group)
            {
                var seq = sequenceExtractor(m.Message);
                if (seq < last)
                {
                    throw new InvalidOperationException($"OrderingAssert.PerKeyMonotonic: key '{group.Key}' saw {seq} after {last}");
                }
                last = seq;
            }
        }
    }
}
