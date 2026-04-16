using Microsoft.Extensions.Configuration;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.Core.Builder;

internal sealed class AutoConnectionSource(
    IRigConnectionSource fixture,
    IConfiguration configuration,
    string configKey) : IRigConnectionSource
{
    public string ConnectionString
    {
        get
        {
            if (EnvironmentDetection.IsRunningInCiCd())
                return fixture.ConnectionString;

            var configValue = configuration[configKey];
            return !string.IsNullOrEmpty(configValue) ? configValue : fixture.ConnectionString;
        }
    }
}
