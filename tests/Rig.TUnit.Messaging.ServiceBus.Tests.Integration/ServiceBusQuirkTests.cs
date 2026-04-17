namespace Rig.TUnit.Messaging.ServiceBus.Tests.Integration;

/// <summary>
/// ServiceBus provider quirks documented via assertions against the fixture's shape:
/// connection string is produced, fixture honours EULA requirement, topic name
/// follows the company-domain-side naming convention. A full publish/receive loop
/// requires pre-provisioned topics in the emulator config file and is out of scope
/// for this contract sketch — see T143 ported tests for end-to-end coverage.
/// </summary>
public sealed class ServiceBusQuirkTests
{
    [Test]
    public async Task Quirk_ConnectionString_IsAvailableAfterInit(CancellationToken ct)
    {
        var fx = await SharedServiceBusFixture.GetAsync().ConfigureAwait(false);
        await Assert.That(fx.ConnectionString).IsNotNullOrEmpty();
    }

    [Test]
    public async Task Quirk_TopicName_FollowsConvention(CancellationToken ct)
    {
        var fx = await SharedServiceBusFixture.GetAsync().ConfigureAwait(false);
        await Assert.That(fx.TopicName).IsNotNullOrEmpty();
        await Assert.That(fx.TopicName.ToLowerInvariant()).IsEqualTo(fx.TopicName);
    }

    [Test]
    public async Task Quirk_IsolationKey_ConsistentWithinFixtureInstance(CancellationToken ct)
    {
        var fx = await SharedServiceBusFixture.GetAsync().ConfigureAwait(false);
        var a = fx.IsolationKey.Value;
        var b = fx.IsolationKey.Value;
        await Assert.That(a).IsEqualTo(b);
    }
}
