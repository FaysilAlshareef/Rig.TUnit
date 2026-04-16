using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Core.Tests.Unit.Builder;

public class RigConnectTests
{
    [Test]
    public async Task FromContainer_ReturnsFixtureItself()
    {
        // Arrange
        var fixture = new FakeFixture("Server=container");

        // Act
        var source = RigConnect.FromContainer(fixture);

        // Assert
        await Assert.That(source).IsSameReferenceAs(fixture);
    }

    [Test]
    public async Task FromConfig_ReturnsSourceThatReadsFromConfiguration()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["Key"] = "Value" })
            .Build();

        // Act
        var source = RigConnect.FromConfig(config, "Key");

        // Assert
        await Assert.That(source).IsNotNull();
        await Assert.That(source.ConnectionString).IsEqualTo("Value");
    }

    [Test]
    public async Task FromOptions_ReturnsSourceThatReadsFromSelector()
    {
        // Arrange
        var options = Options.Create(new TestOpts { Conn = "Server=opts" });

        // Act
        var source = RigConnect.FromOptions(options, o => o.Conn);

        // Assert
        await Assert.That(source).IsNotNull();
        await Assert.That(source.ConnectionString).IsEqualTo("Server=opts");
    }

    [Test]
    public async Task FromValue_ReturnsSourceWrappingRawString()
    {
        // Act
        var source = RigConnect.FromValue("Server=raw");

        // Assert
        await Assert.That(source).IsNotNull();
        await Assert.That(source.ConnectionString).IsEqualTo("Server=raw");
    }

    [Test]
    public async Task Auto_ReturnsSourceThatSwitchesByEnvironment()
    {
        // Arrange
        var fixture = new FakeFixture("Server=container");
        var config = new ConfigurationBuilder().Build();

        // Act
        var source = RigConnect.Auto(fixture, config, "Missing");

        // Assert — no CI, no config → falls back to fixture
        await Assert.That(source).IsNotNull();
        await Assert.That(source.ConnectionString).IsEqualTo("Server=container");
    }

    private sealed class FakeFixture(string connectionString) : IRigConnectionSource
    {
        public string ConnectionString { get; } = connectionString;
    }

    private sealed class TestOpts
    {
        public string Conn { get; set; } = null!;
    }
}
