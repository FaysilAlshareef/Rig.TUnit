using Rig.TUnit.Grpc.Helpers;
using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Helpers;

public class GrpcClientHelperTests
{
    [Test]
    public async Task SendAsync_WithValidRequest_ReturnsExpectedResponse()
    {
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);

        var response = await client.SayHelloAsync(new HelloRequest { Name = "World" });

        await Assert.That(response.Message).IsEqualTo("Hello World");
    }

    [Test]
    public async Task SendAsync_WithDifferentInput_ReturnsDifferentResponse()
    {
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);

        var response = await client.SayHelloAsync(new HelloRequest { Name = "Test" });

        await Assert.That(response.Message).IsEqualTo("Hello Test");
    }

    [Test]
    public async Task GrpcClientHelper_CreateClient_ReturnsTypedChannel()
    {
        await using var factory = new TestGrpcWebApplicationFactory();

        var helper = new GrpcClientHelper<TestService.TestServiceClient, TestProgram>(factory);
        var client = helper.Send(c => c);

        await Assert.That(client).IsNotNull();
        await Assert.That(client).IsTypeOf<TestService.TestServiceClient>();
    }

    [Test]
    public async Task GrpcClientHelper_Send_InvokesGrpcCall()
    {
        await using var factory = new TestGrpcWebApplicationFactory();
        var helper = new GrpcClientHelper<TestService.TestServiceClient, TestProgram>(factory);

        var response = helper.Send(c => c.SayHello(new HelloRequest { Name = "Send" }));

        await Assert.That(response.Message).IsEqualTo("Hello Send");
    }

    [Test]
    public async Task GrpcClientHelper_SendAsync_InvokesGrpcCallAsync()
    {
        await using var factory = new TestGrpcWebApplicationFactory();
        var helper = new GrpcClientHelper<TestService.TestServiceClient, TestProgram>(factory);

        var response = await helper.SendAsync(c => c.SayHelloAsync(new HelloRequest { Name = "SendAsync" }));

        await Assert.That(response.Message).IsEqualTo("Hello SendAsync");
    }
}
