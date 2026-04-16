using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Mediator.Helpers;
using Rig.TUnit.WebAPI.Helpers;

namespace Rig.TUnit.WebAPI.Builder;

/// <summary>
/// Fluent builder that scopes WebAPI-specific registrations to a given
/// <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public sealed class WebApiRigBuilder<TProgram> where TProgram : class
{
    private readonly IServiceCollection _services;
    private readonly WebApplicationFactory<TProgram> _factory;

    internal WebApiRigBuilder(IServiceCollection services, WebApplicationFactory<TProgram> factory)
    {
        _services = services;
        _factory = factory;
    }

    /// <summary>
    /// Registers <see cref="HttpClientHelper{TProgram}"/> as a singleton bound to this factory.
    /// </summary>
    public WebApiRigBuilder<TProgram> AddHttpClientHelper()
    {
        _services.AddSingleton(new HttpClientHelper<TProgram>(_factory));
        return this;
    }

    /// <summary>
    /// Registers <see cref="HandlerHelper"/> as a singleton that resolves the factory's scope factory.
    /// </summary>
    public WebApiRigBuilder<TProgram> AddHandlerHelper()
    {
        _services.AddSingleton(_ =>
            new HandlerHelper(_factory.Services.GetRequiredService<IServiceScopeFactory>()));
        return this;
    }
}
