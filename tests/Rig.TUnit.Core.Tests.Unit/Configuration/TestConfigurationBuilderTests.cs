using Rig.TUnit.Core.Configuration;

namespace Rig.TUnit.Core.Tests.Unit.Configuration;

public class TestConfigurationBuilderTests
{
    [Test]
    public async Task Set_SingleKey_BuildReturnsValue()
    {
        // Arrange & Act
        var config = new TestConfigurationBuilder()
            .Set("MyKey", "MyValue")
            .Build();

        // Assert
        await Assert.That(config["MyKey"]).IsEqualTo("MyValue");
    }

    [Test]
    public async Task SetConnectionString_BuildReturnsCorrectKey()
    {
        // Arrange & Act
        var config = new TestConfigurationBuilder()
            .SetConnectionString("OrderDb", "Server=localhost")
            .Build();

        // Assert
        await Assert.That(config["ConnectionStrings:OrderDb"]).IsEqualTo("Server=localhost");
    }

    [Test]
    public async Task SetSection_BuildReturnsPrefixedKeys()
    {
        // Arrange & Act
        var config = new TestConfigurationBuilder()
            .SetSection("ServiceBus", new Dictionary<string, string>
            {
                ["TopicName"] = "orders",
                ["MaxRetries"] = "3"
            })
            .Build();

        // Assert
        await Assert.That(config["ServiceBus:TopicName"]).IsEqualTo("orders");
        await Assert.That(config["ServiceBus:MaxRetries"]).IsEqualTo("3");
    }

    [Test]
    public async Task BuildOptions_BindsToTypedClass()
    {
        // Arrange & Act
        var options = new TestConfigurationBuilder()
            .SetSection("MySection", new Dictionary<string, string>
            {
                ["Name"] = "Test",
                ["Value"] = "42"
            })
            .BuildOptions<TestOptions>("MySection");

        // Assert
        await Assert.That(options.Name).IsEqualTo("Test");
        await Assert.That(options.Value).IsEqualTo("42");
    }

    [Test]
    public async Task Create_StaticFactory_ReturnsConfiguration()
    {
        // Act
        var config = TestConfigurationBuilder.Create(c => c
            .Set("Key1", "Value1")
            .SetConnectionString("Db", "conn-string"));

        // Assert
        await Assert.That(config["Key1"]).IsEqualTo("Value1");
        await Assert.That(config["ConnectionStrings:Db"]).IsEqualTo("conn-string");
    }

    [Test]
    public async Task Set_MultipleKeys_AllRetrievable()
    {
        // Arrange & Act
        var config = new TestConfigurationBuilder()
            .Set("A", "1")
            .Set("B", "2")
            .Set("C", "3")
            .Build();

        // Assert
        await Assert.That(config["A"]).IsEqualTo("1");
        await Assert.That(config["B"]).IsEqualTo("2");
        await Assert.That(config["C"]).IsEqualTo("3");
    }

    private sealed class TestOptions
    {
        public string Name { get; set; } = null!;
        public string Value { get; set; } = null!;
    }
}
