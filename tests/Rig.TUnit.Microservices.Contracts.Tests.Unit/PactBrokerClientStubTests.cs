using System.Text.Json;
using Rig.TUnit.Microservices.Contracts;
using Rig.TUnit.Microservices.Contracts.Helpers;

namespace Rig.TUnit.Microservices.Contracts.Tests.Unit;

public sealed class PactBrokerClientStubTests
{
    [Test]
    public async Task Ctor_WithNullDir_Throws()
    {
        await Assert.That(() => new PactBrokerClientStub(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DiscoverPacts_OnMissingDir_ReturnsEmpty()
    {
        var stub = new PactBrokerClientStub(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N")));
        var count = stub.DiscoverPacts().Count;
        await Assert.That(count).IsEqualTo(0);
    }

    [Test]
    public async Task Load_WhenFileMissing_Throws()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var stub = new PactBrokerClientStub(dir);
            await Assert.That(() => stub.Load("consumer", "provider"))
                .ThrowsExactly<FileNotFoundException>();
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task Load_WhenFilePresent_ReturnsParsedPact()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var pact = new ContractPact(
                ConsumerName: "orders-consumer",
                ProviderName: "orders-provider",
                Interactions: new[]
                {
                    new ContractInteraction("get list", "GET", "/orders", ResponseStatus: 200),
                });
            var path = Path.Combine(dir, "orders-consumer-orders-provider.json");
            await File.WriteAllTextAsync(path, JsonSerializer.Serialize(pact));

            var stub = new PactBrokerClientStub(dir);
            var loaded = stub.Load("orders-consumer", "orders-provider");

            await Assert.That(loaded.ConsumerName).IsEqualTo("orders-consumer");
            await Assert.That(loaded.ProviderName).IsEqualTo("orders-provider");
            await Assert.That(loaded.Interactions.Count).IsEqualTo(1);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }

    [Test]
    public async Task DiscoverPacts_ListsAllJsonFiles()
    {
        var dir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            await File.WriteAllTextAsync(Path.Combine(dir, "a.json"), "{}");
            await File.WriteAllTextAsync(Path.Combine(dir, "b.json"), "{}");
            await File.WriteAllTextAsync(Path.Combine(dir, "c.txt"), "x");

            var stub = new PactBrokerClientStub(dir);
            var names = stub.DiscoverPacts();

            await Assert.That(names.Count).IsEqualTo(2);
        }
        finally { Directory.Delete(dir, recursive: true); }
    }
}
