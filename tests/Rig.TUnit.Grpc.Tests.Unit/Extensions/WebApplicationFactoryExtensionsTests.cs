using Grpc.Net.Client;
using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Extensions;

public class WebApplicationFactoryExtensionsTests
{
    [Test]
    public async Task CreateGrpcChannel_ReturnsValidChannel()
    {
        // Arrange
        await using var server = new TestServerFactory();

        // Act
        var channel = server.CreateGrpcChannel();

        // Assert
        await Assert.That(channel).IsNotNull();
        await Assert.That(channel).IsTypeOf<GrpcChannel>();
    }

    [Test]
    public async Task CreateGrpcChannel_CanMakeGrpcCalls()
    {
        // Arrange
        await using var server = new TestServerFactory();

        // Act
        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Channel" });

        // Assert
        await Assert.That(response.Message).IsEqualTo("Hello Channel");
    }

    [Test]
    public async Task TestServer_ProvidesServiceProvider()
    {
        // Arrange
        await using var server = new TestServerFactory();

        // Act
        var services = server.Services;

        // Assert
        await Assert.That(services).IsNotNull();
    }

    [Test]
    public async Task TestServer_CreatesDefaultClient()
    {
        // Arrange
        await using var server = new TestServerFactory();

        // Act
        var client = server.CreateDefaultClient();

        // Assert
        await Assert.That(client).IsNotNull();
        await Assert.That(client.BaseAddress).IsNotNull();
    }
}
