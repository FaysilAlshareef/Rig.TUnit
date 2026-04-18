using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.NoSql.ElasticSearch.Options;

public sealed class ElasticSearchFixtureOptions
{
    public const string SectionName = "RigTUnit:ElasticSearch";

    [Required]
    public string ImageTag { get; init; } = "8.15.3";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 360;
}
