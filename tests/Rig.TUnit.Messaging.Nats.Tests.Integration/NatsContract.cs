using Rig.TUnit.Core;
using Rig.TUnit.Messaging.Contracts;
using Rig.TUnit.Messaging.Tests.Contract;

namespace Rig.TUnit.Messaging.Nats.Tests.Integration;

[InheritsTests]
public sealed class NatsContract : MessagingRigContract
{
    protected override async ValueTask<IMessagingRig> CreateMessagingRigAsync(CancellationToken ct)
        => await SharedNatsFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IMessagingRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_TopicName_IsUniquePerRun()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.Value).IsNotEqualTo(k2.Value);
    }
}
