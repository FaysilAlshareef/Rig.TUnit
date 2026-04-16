using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Rig.TUnit.Core.Builder;

public static class RigConnect
{
    /// <summary>From a container fixture (Testcontainers).</summary>
    public static IRigConnectionSource FromContainer(IRigConnectionSource fixture)
        => fixture;

    /// <summary>From an IConfiguration key (e.g., "ConnectionStrings:OrderDb").</summary>
    public static IRigConnectionSource FromConfig(IConfiguration configuration, string key)
        => new ConfigConnectionSource(configuration, key);

    /// <summary>From an IOptions&lt;T&gt; property selector.</summary>
    public static IRigConnectionSource FromOptions<TOptions>(
        IOptions<TOptions> options,
        Func<TOptions, string> selector) where TOptions : class
        => new OptionsConnectionSource<TOptions>(options, selector);

    /// <summary>From a raw connection string value.</summary>
    public static IRigConnectionSource FromValue(string connectionString)
        => new ValueConnectionSource(connectionString);

    /// <summary>
    /// Smart mode: uses container in CI/CD, falls back to configuration locally.
    /// </summary>
    public static IRigConnectionSource Auto(
        IRigConnectionSource fixture,
        IConfiguration configuration,
        string configKey)
        => new AutoConnectionSource(fixture, configuration, configKey);
}
