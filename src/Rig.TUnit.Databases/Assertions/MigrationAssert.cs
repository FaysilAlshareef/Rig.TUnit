using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Assertions;

/// <summary>
/// Provider-agnostic migration assertions. Concrete providers implement
/// <see cref="IMigrationProbe"/> to bridge to their native migration runner.
/// </summary>
public static class MigrationAssert
{
    public static Task AllApplied(IDbRig rig, IMigrationProbe probe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.AllAppliedAsync(rig, ct);
    }

    public static Task NoPendingModelChanges(IDbRig rig, IMigrationProbe probe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.NoPendingModelChangesAsync(rig, ct);
    }

    public static Task Idempotent(IDbRig rig, IMigrationProbe probe, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.IdempotentAsync(rig, ct);
    }
}

public interface IMigrationProbe
{
    Task AllAppliedAsync(IDbRig rig, CancellationToken ct);
    Task NoPendingModelChangesAsync(IDbRig rig, CancellationToken ct);
    Task IdempotentAsync(IDbRig rig, CancellationToken ct);
}
