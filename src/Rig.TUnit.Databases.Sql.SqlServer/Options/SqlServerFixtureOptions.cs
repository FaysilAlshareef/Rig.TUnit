using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.Sql.SqlServer.Options;

/// <summary>
/// Bound configuration for <c>SqlServerFixture</c>. Registered via
/// <c>services.AddOptions&lt;SqlServerFixtureOptions&gt;().BindConfiguration(SectionName)
/// .ValidateDataAnnotations().ValidateOnStart()</c>.
/// </summary>
public sealed class SqlServerFixtureOptions
{
    public const string SectionName = "RigTUnit:SqlServer";

    [Required]
    public string ImageTag { get; init; } = "2022-latest";

    [Range(1, 600)]
    public int StartupTimeoutSeconds { get; init; } = 120;

    [Required]
    public string SaPassword { get; init; } = "Your_password123!";
}
