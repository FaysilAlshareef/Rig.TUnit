using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Helpers;

public class GrpcClientHelperTests
{
    [Test]
    public async Task SendAsync_WithValidRequest_ReturnsExpectedResponse()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);

        // Act
        var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });

        // Assert
        await Assert.That(response.Message).IsEqualTo("Hello World");
    }

    [Test]
    public async Task SendAsync_WithDifferentInput_ReturnsDifferentResponse()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);

        // Act
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Test" });

        // Assert
        await Assert.That(response.Message).IsEqualTo("Hello Test");
    }
}
