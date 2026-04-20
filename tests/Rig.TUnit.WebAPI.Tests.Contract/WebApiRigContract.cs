using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Rig.TUnit.WebAPI.Helpers;

namespace Rig.TUnit.WebAPI.Tests.Contract;

/// <summary>
/// Base contract every WebAPI rig suite inherits. Asserts the helper's happy-path
/// invariants against a minimal in-memory factory — provider suites override the
/// factory via <see cref="CreateFactory"/> to wire real authentication / DI.
/// </summary>
public abstract class WebApiRigContract
{
    protected virtual WebApplicationFactory<WebApiRigContract.ContractProgram> CreateFactory()
        => new ContractFactory();

    [Test]
    public async Task Helper_ExposesNonNullClient()
    {
        await using var factory = CreateFactory();
        var helper = new HttpClientHelper<ContractProgram>(factory);

        await Assert.That(helper.Client).IsNotNull();
    }

    [Test]
    public async Task Helper_WithBearerToken_SetsAuthorizationScheme()
    {
        await using var factory = CreateFactory();
        var helper = new HttpClientHelper<ContractProgram>(factory);

        helper.WithBearerToken("token");

        await Assert.That(helper.Client.DefaultRequestHeaders.Authorization).IsNotNull();
    }

    public sealed class ContractProgram;

    private sealed class ContractFactory : WebApplicationFactory<ContractProgram>
    {
        protected override IHostBuilder CreateHostBuilder()
        {
            return Host.CreateDefaultBuilder()
                .UseContentRoot(AppContext.BaseDirectory)
                .ConfigureWebHostDefaults(webHost =>
                {
                    webHost.UseTestServer();
                    webHost.ConfigureServices(services => services.AddRouting());
                    webHost.Configure(_ => { });
                });
        }

        protected override IHost CreateHost(IHostBuilder builder)
        {
            builder.UseContentRoot(AppContext.BaseDirectory);
            return base.CreateHost(builder);
        }
    }
}
