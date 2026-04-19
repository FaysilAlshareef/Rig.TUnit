using System.Collections.Concurrent;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Helpers;

/// <summary>
/// Captures RU charges reported by Cosmos SDK responses so tests can assert
/// (a) total RU per logical operation, (b) per-operation RU budget, (c)
/// distribution to detect unexpected spikes from a refactor.
///
/// The Cosmos SDK exposes <c>response.RequestCharge</c> on every result — call
/// <see cref="Record"/> after each SDK call. <see cref="TotalRu"/> and
/// <see cref="Samples"/> are thread-safe.
/// </summary>
public sealed class RuChargeCapture
{
    private readonly ConcurrentBag<RuChargeSample> _samples = new();

    public IReadOnlyCollection<RuChargeSample> Samples => _samples;

    public double TotalRu => _samples.Sum(s => s.RequestCharge);

    public void Record(string operation, double requestCharge)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(operation);
        if (requestCharge < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(requestCharge), requestCharge, "RU charge must be non-negative.");
        }
        _samples.Add(new RuChargeSample(operation, requestCharge));
    }

    public void Clear()
    {
        while (_samples.TryTake(out _)) { }
    }
}

public sealed record RuChargeSample(string Operation, double RequestCharge);
