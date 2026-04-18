using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Options;

public sealed class CassandraFixtureOptions
{
    public const string SectionName = "RigTUnit:Cassandra";

    [Required]
    public string ImageTag { get; init; } = "5";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 360;
}
