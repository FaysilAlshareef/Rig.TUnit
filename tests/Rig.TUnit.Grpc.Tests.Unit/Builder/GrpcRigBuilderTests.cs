using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Grpc.Builder;
using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Builder;

public class GrpcRigBuilderTests
{
    [Test]
    public async Task ReplaceClient_ViaBuilder_RouteThroughTestServer()
    {
        // Arrange
        await using var factory = new TestGrpcWebApplicationFactory();
        factory.CreateClient(); // force host start before capturing services

        var services = new ServiceCollection();
        services.AddSingleton<TestService.TestServiceClient>(_ => null!);

        // Act
        services.AddRigTUnit(rig =>
            rig.UseGrpc(factory, grpc => grpc.ReplaceClient<TestService.TestServiceClient>()));

        // Assert
        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestService.TestServiceClient>();
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Builder" });
        await Assert.That(response.Message).IsEqualTo("Hello Builder");
    }
}
