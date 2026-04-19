using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.Mongo.Options;

public sealed class MongoFixtureOptions
{
    public const string SectionName = "RigTUnit:Mongo";

    [Required]
    public string ImageTag { get; init; } = "7";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 360;

    [Required]
    public string Username { get; init; } = "root";

    [Required]
    public string Password { get; init; } = "mongo";
}
