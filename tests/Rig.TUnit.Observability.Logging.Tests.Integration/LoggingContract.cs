using Rig.TUnit.Observability.Contracts;
using Rig.TUnit.Observability.Logging.Fixtures;
using Rig.TUnit.Observability.Logging.Options;
using Rig.TUnit.Observability.Tests.Contract;

namespace Rig.TUnit.Observability.Logging.Tests.Integration;

[InheritsTests]
public sealed class LoggingContract : TelemetryRigContract
{
    protected override async ValueTask<ITelemetryRig> CreateTelemetryRigAsync(CancellationToken ct)
    {
        var fx = new LoggingFixture(new LoggingFixtureOptions());
        await fx.InitializeAsync();
        return fx;
    }
}
