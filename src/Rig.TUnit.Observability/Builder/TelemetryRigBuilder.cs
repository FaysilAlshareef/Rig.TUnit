using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Observability.Builder;

/// <summary>
/// Base builder for telemetry providers (tracing, logging, Seq). Mirrors the shape
/// of <c>MessagingRigBuilder</c> / <c>CacheRigBuilder</c> / <c>DatabaseRigBuilder</c>
/// so consumers get a consistent <c>.UseX(...).And()</c> fluent chain.
/// </summary>
public abstract class TelemetryRigBuilder<TSelf> where TSelf : TelemetryRigBuilder<TSelf>
{
    protected TelemetryRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    protected RigBuilder Root { get; }
    protected IRigConnectionSource Source { get; }

    /// <summary>Returns the parent <see cref="RigBuilder"/> so additional rigs can be chained.</summary>
    public RigBuilder And() => Root;
}
