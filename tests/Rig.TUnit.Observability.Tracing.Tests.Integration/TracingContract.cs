using Rig.TUnit.Observability.Contracts;
using Rig.TUnit.Observability.Tests.Contract;
using Rig.TUnit.Observability.Tracing.Fixtures;
using Rig.TUnit.Observability.Tracing.Options;

namespace Rig.TUnit.Observability.Tracing.Tests.Integration;

/// <summary>
/// Binds <see cref="TelemetryRigContract"/> against a real in-memory OTEL TracingFixture.
/// </summary>
[InheritsTests]
public sealed class TracingContract : TelemetryRigContract
{
    protected override async ValueTask<ITelemetryRig> CreateTelemetryRigAsync(CancellationToken ct)
    {
        var fixture = new TracingFixture(new TracingFixtureOptions
        {
            ServiceName = "tracing-contract",
            SampleRatio = 1.0,
            MaxSpansInMemory = 1000,
        });
        await fixture.InitializeAsync();
        return fixture;
    }
}
