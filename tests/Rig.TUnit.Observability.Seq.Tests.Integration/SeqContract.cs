using Rig.TUnit.Core;
using Rig.TUnit.Observability.Contracts;
using Rig.TUnit.Observability.Tests.Contract;

namespace Rig.TUnit.Observability.Seq.Tests.Integration;

/// <summary>
/// Binds <see cref="TelemetryRigContract"/> against a shared Seq Testcontainer.
/// Requires Docker. Share-container pattern means
/// <see cref="TelemetryRigContract.Fixture_ServiceName_IsUniquePerRun"/> is overridden
/// to assert uniqueness via <see cref="IsolationKey"/> rather than two fresh fixtures.
/// </summary>
[InheritsTests]
public sealed class SeqContract : TelemetryRigContract
{
    protected override async ValueTask<ITelemetryRig> CreateTelemetryRigAsync(CancellationToken ct)
        => await SharedSeqFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(ITelemetryRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_ServiceName_IsUniquePerRun()
    {
        var a = IsolationKey.FromName(Guid.NewGuid().ToString()).Value;
        var b = IsolationKey.FromName(Guid.NewGuid().ToString()).Value;
        await Assert.That(a).IsNotEqualTo(b);
    }
}
