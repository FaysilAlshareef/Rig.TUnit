using Rig.TUnit.Docker.Fixtures;

namespace Rig.TUnit.Docker.Tests.Integration;

/// <summary>
/// Live Docker-based tests. Exercises a basic alpine container round-trip and
/// asserts that the Hostname surfaces after startup.
/// </summary>
[Category("docker")]
public sealed class ContainerFixtureLiveTests
{
    [Test]
    public async Task InitializeAsync_AlpineEcho_HostnameAvailable()
    {
        await using var fx = new ContainerFixture(image: "alpine:3");
        await fx.InitializeAsync();

        var host = fx.ConnectionString;
        await Assert.That(host).IsNotNullOrEmpty();
    }
}
