using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Caching.Redis.Options;

public sealed class RedisFixtureOptions
{
    public const string SectionName = "RigTUnit:Redis";

    [Required]
    public string ImageTag { get; init; } = "7-alpine";

    [Range(0, 15)]
    public int Database { get; init; }

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 60;
}
