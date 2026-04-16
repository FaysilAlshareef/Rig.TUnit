using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Mediator.Helpers;
using Rig.TUnit.WebAPI.Builder;
using Rig.TUnit.WebAPI.Helpers;
using Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.WebAPI.Tests.Unit.Builder;

public sealed class WebApiRigBuilderTests
{
    [Test]
    public async Task UseWebApi_AddHttpClientHelper_RegistersInServiceCollection()
    {
        // Arrange
        var services = new ServiceCollection();
        await using var factory = new TestWebApplicationFactory();

        // Act
        services.AddRigTUnit(rig =>
        {
            rig.UseWebApi(factory, api => api.AddHttpClientHelper());
        });

        // Assert
        await using var provider = services.BuildServiceProvider();
        var helper = provider.GetService<HttpClientHelper<TestProgram>>();
        await Assert.That(helper).IsNotNull();
    }

    [Test]
    public async Task UseWebApi_AddHandlerHelper_RegistersHandlerHelper()
    {
        // Arrange
        var services = new ServiceCollection();
        await using var factory = new TestWebApplicationFactory();

        // Act
        services.AddRigTUnit(rig =>
        {
            rig.UseWebApi(factory, api => api.AddHandlerHelper());
        });

        // Assert
        await using var provider = services.BuildServiceProvider();
        var helper = provider.GetService<HandlerHelper>();
        await Assert.That(helper).IsNotNull();
    }
}
