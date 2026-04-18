using Rig.TUnit.Core;
using Rig.TUnit.Core.Fixtures;
using Rig.TUnit.Observability.Contracts;

namespace Rig.TUnit.Observability.Fixtures;

/// <summary>
/// Base class for telemetry fixtures (tracing, logging, Seq). Provides a stable
/// <see cref="IsolationKey"/> and a service name derived from it so parallel fixtures
/// publish distinct <c>service.name</c> / log-scope values and never cross-contaminate.
/// Concrete fixtures override <see cref="RigFixtureBase.ConnectionString"/>,
/// <see cref="RigFixtureBase.InitializeAsync"/>, and
/// <see cref="RigFixtureBase.DisposeAsync"/> as appropriate for their exporter/sink.
/// </summary>
public abstract class TelemetryFixtureBase : RigFixtureBase, ITelemetryRig
{
    protected TelemetryFixtureBase()
    {
        IsolationKey = IsolationKey.FromExecutionContext();
    }

    public IsolationKey IsolationKey { get; }

    /// <summary>
    /// Service name derived from <see cref="IsolationKey"/>. Override to provide a
    /// custom value (e.g. from options). Safe to use as an OTEL <c>service.name</c>
    /// resource attribute or a Seq <c>Application</c> property.
    /// </summary>
    public virtual string ServiceName => $"rigtunit-{IsolationKey.Value}";
}
