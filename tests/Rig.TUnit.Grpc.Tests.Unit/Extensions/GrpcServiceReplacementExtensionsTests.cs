using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Extensions;
using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Extensions;

public class GrpcServiceReplacementExtensionsTests
{
    [Test]
    public async Task ReplaceGrpcClient_ReplacesExisting_RoutesThroughTestServer()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var testClient = new TestService.TestServiceClient(channel);

        var services = new ServiceCollection();
        services.AddSingleton<TestService.TestServiceClient>(_ => null!);

        // Act -- replace the null registration with a real test client
        services.RemoveService<TestService.TestServiceClient>();
        services.AddSingleton(testClient);

        // Assert
        var provider = services.BuildServiceProvider();
        var resolved = provider.GetService<TestService.TestServiceClient>();
        await Assert.That(resolved).IsNotNull();
    }

    [Test]
    public async Task ReplaceGrpcClient_ClientCanMakeGrpcCalls()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var channel = server.CreateGrpcChannel();
        var testClient = new TestService.TestServiceClient(channel);

        var services = new ServiceCollection();
        services.AddSingleton<TestService.TestServiceClient>(_ => null!);

        // Act
        services.RemoveService<TestService.TestServiceClient>();
        services.AddSingleton(testClient);
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestService.TestServiceClient>();
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Replaced" });

        // Assert
        await Assert.That(response.Message).IsEqualTo("Hello Replaced");
    }
}
