using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Observability.Contracts;

/// <summary>
/// Contract implemented by every telemetry fixture — tracing, logging, Seq, metrics.
/// Tracing fixtures expose in-memory OTEL exporters; logging fixtures expose in-memory
/// <c>ILoggerProvider</c> captures; Seq fixtures expose a Testcontainer-hosted sink.
/// </summary>
public interface ITelemetryRig : IRigConnectionSource
{
    /// <summary>
    /// Deterministic, per-test isolation key used to scope service names, log scopes,
    /// and trace resource attributes so parallel fixtures do not cross-contaminate.
    /// </summary>
    IsolationKey IsolationKey { get; }

    /// <summary>
    /// Logical service name attached to traces / logs / Seq sinks for this fixture.
    /// Typically derived from <see cref="IsolationKey"/> so twenty parallel fixtures
    /// produce twenty distinct <c>service.name</c> values.
    /// </summary>
    string ServiceName { get; }
}
