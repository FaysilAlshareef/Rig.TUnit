namespace Rig.TUnit.Messaging.Assertions;

public sealed class DeadLetterAssert
{
    /// <summary>
    /// Default DLQ probe window. Conservative for the Microsoft Service Bus
    /// emulator (and equivalent containerised brokers), where DLQ
    /// materialisation after max-delivery-count exhaustion lags real Azure
    /// Service Bus by several seconds.
    /// </summary>
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(60);

    private DeadLetterAssert() { }

    public static Task HasMessage(
        IDeadLetterProbe probe,
        string expectedReason,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReason);
        return probe.HasMessageAsync(expectedReason, timeout ?? DefaultTimeout, ct);
    }

    public static Task IsEmpty(IDeadLetterProbe probe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.IsEmptyAsync(ct);
    }
}

public interface IDeadLetterProbe
{
    Task HasMessageAsync(string expectedReason, TimeSpan timeout, CancellationToken ct);
    Task IsEmptyAsync(CancellationToken ct);
}
