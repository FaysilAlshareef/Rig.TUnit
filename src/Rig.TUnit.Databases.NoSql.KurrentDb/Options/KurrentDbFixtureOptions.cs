using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Options;

public sealed class KurrentDbFixtureOptions
{
    public const string SectionName = "RigTUnit:KurrentDb";

    [Required]
    public string ImageTag { get; init; } = "25.1";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 300;
}
