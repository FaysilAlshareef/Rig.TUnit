using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.WebAPI.Extensions;

/// <summary>
/// Extension methods for <see cref="WebApplicationFactory{TEntryPoint}"/> that simplify
/// common test setup: replacing services and injecting in-memory configuration.
/// </summary>
public static class WebApiFactoryExtensions
{
    /// <summary>
    /// Configures test services and optionally layers an in-memory <see cref="IConfiguration"/> source
    /// on top of the application configuration.
    /// </summary>
    /// <param name="factory">The WebApplicationFactory to configure.</param>
    /// <param name="configureServices">Delegate to register or replace services in the test host.</param>
    /// <param name="configuration">Optional in-memory configuration key/value pairs added on top of the existing configuration.</param>
    public static WebApplicationFactory<TProgram> WithTestServices<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<IServiceCollection> configureServices,
        IDictionary<string, string?>? configuration = null) where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(configureServices);

        return factory.WithWebHostBuilder(builder =>
        {
            if (configuration is not null)
            {
                builder.ConfigureAppConfiguration((_, configBuilder) =>
                {
                    configBuilder.AddInMemoryCollection(configuration);
                });
            }

            builder.ConfigureTestServices(configureServices);
        });
    }
}
