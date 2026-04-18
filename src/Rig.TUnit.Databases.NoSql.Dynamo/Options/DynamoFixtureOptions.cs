using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Options;

public sealed class DynamoFixtureOptions
{
    public const string SectionName = "RigTUnit:Dynamo";

    [Required]
    public string ImageTag { get; init; } = "3";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 180;

    [Required]
    public string Region { get; init; } = "us-east-1";
}
