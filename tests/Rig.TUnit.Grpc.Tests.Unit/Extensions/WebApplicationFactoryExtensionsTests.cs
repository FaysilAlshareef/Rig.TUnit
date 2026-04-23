using Grpc.Net.Client;
using Microsoft.AspNetCore.Builder;
using Rig.TUnit.Grpc.Extensions;
using Rig.TUnit.Grpc.Tests.Unit.Protos;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Extensions;

public class WebApplicationFactoryExtensionsTests
{
    [Test]
    public async Task CreateGrpcChannel_ReturnsValidChannel()
    {
        await using var server = new TestServerFactory();

        var channel = server.CreateGrpcChannel();

        await Assert.That(channel).IsNotNull();
        await Assert.That(channel).IsTypeOf<GrpcChannel>();
    }

    [Test]
    public async Task CreateGrpcChannel_CanMakeGrpcCalls()
    {
        await using var server = new TestServerFactory();

        var channel = server.CreateGrpcChannel();
        var client = new TestService.TestServiceClient(channel);
        var response = await client.SayHelloAsync(new HelloRequest { Name = "Channel" });

        await Assert.That(response.Message).IsEqualTo("Hello Channel");
    }

    [Test]
    public async Task TestServer_ProvidesServiceProvider()
    {
        await using var server = new TestServerFactory();

        var services = server.Services;

        await Assert.That(services).IsNotNull();
    }

    [Test]
    public async Task TestServer_CreatesDefaultClient()
    {
        await using var server = new TestServerFactory();

        var client = server.CreateDefaultClient();

        await Assert.That(client).IsNotNull();
        await Assert.That(client.BaseAddress).IsNotNull();
    }

    [Test]
    public async Task WebApplicationFactoryExtensions_CreateGrpcChannel_ReturnsWorkingChannel()
    {
        await using var factory = new TestGrpcWebApplicationFactory();

        using var channel = factory.CreateGrpcChannel();

        await Assert.That(channel).IsNotNull();
        await Assert.That(channel.Target).IsNotNull();
    }

    [Test]
    public async Task WithTestConfiguration_WithConfigureServices_CallsConfigureServices()
    {
        var configureServicesCalled = false;
        await using var baseFactory = new TestGrpcWebApplicationFactory();
        await using var factory = baseFactory.WithTestConfiguration(
            configureServices: _ => { configureServicesCalled = true; });

        factory.CreateClient();

        await Assert.That(configureServicesCalled).IsTrue();
    }

    [Test]
    public async Task EndpointMappingStartupFilter_Configure_RegistersEndpoints()
    {
        await using var baseFactory = new MinimalTestFactory();
        await using var factory = baseFactory.WithTestConfiguration(
            mapEndpoints: endpoints => endpoints.MapGet("/ping", () => "pong"));

        using var client = factory.CreateDefaultClient();
        var response = await client.GetAsync("/ping");

        await Assert.That((int)response.StatusCode).IsEqualTo(200);
    }
}
