using Rig.TUnit.ServiceBus.Fixtures;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Fixtures;

[NotInParallel("ServiceBus")]
public class ServiceBusFixtureTests
{
    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task InitializeAsync_StartsEmulator(ServiceBusFixture fixture)
    {
        await Assert.That(fixture.ConnectionString).IsNotNull();
        await Assert.That(fixture.ConnectionString.Length).IsGreaterThan(0);
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task ConnectionString_IsValidFormat(ServiceBusFixture fixture)
    {
        await Assert.That(fixture.ConnectionString).Contains("Endpoint=");
    }
}
