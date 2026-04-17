using Rig.TUnit.Messaging.Contracts;
using Rig.TUnit.Messaging.Tests.Contract;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration;

/// <summary>
/// Concrete <see cref="MessagingRigContract"/> binding the Azure Service Bus emulator.
/// Shares the assembly-wide container so only one emulator boot per test run.
/// </summary>
[InheritsTests]
public sealed class ServiceBusContract : MessagingRigContract
{
    protected override async ValueTask<IMessagingRig> CreateMessagingRigAsync(CancellationToken ct)
        => await SharedServiceBusFixture.GetAsync().ConfigureAwait(false);

    protected override ValueTask DisposeRigAsync(IMessagingRig rig) => ValueTask.CompletedTask;

    public override async Task Fixture_TopicName_IsUniquePerRun()
    {
        var k1 = Rig.TUnit.Core.IsolationKey.FromName(Guid.NewGuid().ToString());
        var k2 = Rig.TUnit.Core.IsolationKey.FromName(Guid.NewGuid().ToString());
        await Assert.That(k1.Value).IsNotEqualTo(k2.Value);
    }
}
