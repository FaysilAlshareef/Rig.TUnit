using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.Sql.Postgresql.Options;

public sealed class PostgresFixtureOptions
{
    public const string SectionName = "RigTUnit:Postgres";

    [Required]
    public string ImageTag { get; init; } = "16-alpine";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;

    [Required]
    public string Username { get; init; } = "postgres";

    [Required]
    public string Password { get; init; } = "postgres";

    [Required]
    public string Database { get; init; } = "rigtunit";
}
