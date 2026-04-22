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
        await using var factory = new TestGrpcWebApplicationFactory();
        factory.CreateClient();

        var services = new ServiceCollection();
        services.AddSingleton<TestService.TestServiceClient>(_ => null!);

        services.AddRigTUnit(rig =>
            rig.UseGrpc(factory, grpc => grpc.ReplaceClient<TestService.TestServiceClient>()));

        var provider = services.BuildServiceProvider();
        var client = provider.GetRequiredService<TestService.TestServiceClient>();
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Builder" });
        await Assert.That(response.Message).IsEqualTo("Hello Builder");
    }

    [Test]
    public async Task UseGrpc_NullRigBuilder_ThrowsArgumentNullException()
    {
        await using var factory = new TestGrpcWebApplicationFactory();

        await Assert.That(() => ((RigBuilder)null!).UseGrpc(factory, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseGrpc_NullFactory_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);

        await Assert.That(() => captured!.UseGrpc((TestGrpcWebApplicationFactory)null!, _ => { }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseGrpc_NullConfigure_ThrowsArgumentNullException()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await using var factory = new TestGrpcWebApplicationFactory();

        await Assert.That(() => captured!.UseGrpc(factory, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task UseGrpc_WithValidArgs_ReturnsSameRigBuilder()
    {
        RigBuilder? captured = null;
        new ServiceCollection().AddRigTUnit(rig => captured = rig);
        await using var factory = new TestGrpcWebApplicationFactory();

        var result = captured!.UseGrpc(factory, _ => { });

        await Assert.That(result).IsSameReferenceAs(captured);
    }
}
