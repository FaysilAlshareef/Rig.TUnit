namespace Rig.TUnit.Messaging.Assertions;

public sealed class DeadLetterAssert
{
    private DeadLetterAssert() { }

    public static Task HasMessage(IDeadLetterProbe probe, string expectedReason, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        ArgumentException.ThrowIfNullOrWhiteSpace(expectedReason);
        return probe.HasMessageAsync(expectedReason, ct);
    }

    public static Task IsEmpty(IDeadLetterProbe probe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(probe);
        return probe.IsEmptyAsync(ct);
    }
}

public interface IDeadLetterProbe
{
    Task HasMessageAsync(string expectedReason, CancellationToken ct);
    Task IsEmptyAsync(CancellationToken ct);
}
