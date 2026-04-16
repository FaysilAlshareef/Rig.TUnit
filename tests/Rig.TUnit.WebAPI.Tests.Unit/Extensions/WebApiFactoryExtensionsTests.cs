using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.WebAPI.Extensions;
using Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.WebAPI.Tests.Unit.Extensions;

public sealed class WebApiFactoryExtensionsTests
{
    private sealed class MarkerService { }

    [Test]
    public async Task WithTestServices_ConfiguresServices()
    {
        // Arrange
        await using var factory = new TestWebApplicationFactory()
            .WithTestServices(services =>
            {
                services.AddSingleton<MarkerService>();
            });

        // Act — force app build
        using var client = factory.CreateClient();
        var marker = factory.Services.GetService<MarkerService>();

        // Assert
        await Assert.That(marker).IsNotNull();
    }

    [Test]
    public async Task WithTestServices_WithConfiguration_AddsInMemoryConfig()
    {
        // Arrange
        var config = new Dictionary<string, string?>
        {
            ["MySetting:Value"] = "from-in-memory"
        };

        await using var factory = new TestWebApplicationFactory()
            .WithTestServices(_ => { }, config);

        // Act — force app build
        using var client = factory.CreateClient();
        var configuration = factory.Services.GetRequiredService<IConfiguration>();

        // Assert
        await Assert.That(configuration["MySetting:Value"]).IsEqualTo("from-in-memory");
    }
}
