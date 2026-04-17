using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.Sql.Sqlite.Options;

public sealed class SqliteFixtureOptions
{
    public const string SectionName = "RigTUnit:Sqlite";

    [Required]
    public string DatabaseName { get; init; } = "rigtunit";

    public string CacheMode { get; init; } = "Shared";

    public bool ForeignKeys { get; init; } = true;
}
