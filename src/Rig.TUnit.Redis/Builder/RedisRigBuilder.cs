using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Extensions;
using StackExchange.Redis;

namespace Rig.TUnit.Redis.Builder;

/// <summary>
/// Fluent builder that wires Redis test infrastructure into an <see cref="IServiceCollection"/>.
/// </summary>
public sealed class RedisRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _source;

    internal RedisRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _source = source;
    }

    /// <summary>
    /// Replaces the default <see cref="IConnectionMultiplexer"/> registration with a Singleton
    /// connected to the source connection string.
    /// </summary>
    public RedisRigBuilder ReplaceMultiplexer()
    {
        _services.RemoveService<IConnectionMultiplexer>();
        _services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(_source.ConnectionString));
        return this;
    }

    /// <summary>
    /// Replaces a custom Redis-backed client registration with a factory that receives the underlying
    /// <see cref="IConnectionMultiplexer"/>. The multiplexer itself is registered as a Singleton.
    /// </summary>
    public RedisRigBuilder ReplaceClient<TClient>(Func<IConnectionMultiplexer, TClient> factory)
        where TClient : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        ReplaceMultiplexer();

        _services.RemoveService<TClient>();
        _services.AddSingleton<TClient>(sp =>
            factory(sp.GetRequiredService<IConnectionMultiplexer>()));

        return this;
    }
}
