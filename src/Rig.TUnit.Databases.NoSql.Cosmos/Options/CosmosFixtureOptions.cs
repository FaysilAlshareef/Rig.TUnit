using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Options;

public sealed class CosmosFixtureOptions
{
    public const string SectionName = "RigTUnit:Cosmos";

    /// <summary>
    /// Docker image for the Linux Cosmos emulator. The `vnext-preview` track is
    /// the only one compatible with testcontainers-dotnet (see testcontainers-dotnet#1306);
    /// the legacy Windows emulator is unsupported.
    /// </summary>
    [Required]
    public string Image { get; init; } = "mcr.microsoft.com/cosmosdb/linux/azure-cosmos-emulator:vnext-preview";

    [Range(60, 600)]
    public int StartupTimeoutSeconds { get; init; } = 300;

    [Required]
    public string DatabaseName { get; init; } = "rigtunit";

    [Range(1, 65535)]
    public int HttpsGatewayPort { get; init; } = 8081;
}
