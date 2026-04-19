using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.Sql.MySql.Options;

public sealed class MySqlFixtureOptions
{
    public const string SectionName = "RigTUnit:MySql";

    [Required]
    public string ImageTag { get; init; } = "8.4";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 180;

    [Required]
    public string Username { get; init; } = "root";

    [Required]
    public string Password { get; init; } = "rigtunit";

    [Required]
    public string Database { get; init; } = "rigtunit";
}
