using Rig.TUnit.ServiceBus.Fixtures;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Fixtures;

/// <summary>
/// Construction-time tests for <see cref="ServiceBusFixture.ConfigFilePath"/>.
/// These do not start a container and can run without Docker.
/// </summary>
public class ServiceBusFixtureConfigPathTests
{
    [Test]
    public async Task ConfigFilePath_DefaultsToTestInfrastructurePath()
    {
        // Arrange & Act
        var fixture = new ServiceBusFixture();

        // Assert
        await Assert.That(fixture.ConfigFilePath).IsEqualTo("TestInfrastructure/service-bus-config.json");
    }

    [Test]
    public async Task ConfigFilePath_CanBeSetViaInit()
    {
        // Arrange & Act
        var fixture = new ServiceBusFixture { ConfigFilePath = "custom/path.json" };

        // Assert
        await Assert.That(fixture.ConfigFilePath).IsEqualTo("custom/path.json");
    }
}
