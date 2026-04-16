using Microsoft.Extensions.Configuration;

namespace Rig.TUnit.Core.Builder;

internal sealed class ConfigConnectionSource(IConfiguration configuration, string key) : IRigConnectionSource
{
    public string ConnectionString => configuration[key]
        ?? throw new InvalidOperationException(
            $"Configuration key '{key}' not found. Ensure it exists in appsettings or user secrets.");
}
