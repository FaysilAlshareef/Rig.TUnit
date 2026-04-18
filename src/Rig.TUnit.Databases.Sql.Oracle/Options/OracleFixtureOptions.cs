using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Databases.Sql.Oracle.Options;

public sealed class OracleFixtureOptions
{
    public const string SectionName = "RigTUnit:Oracle";

    /// <summary>
    /// Image tag. Defaults to <c>gvenzl/oracle-free:23.5-slim-faststart</c> —
    /// Oracle Free is the community-maintained light image that boots in ~60-90s
    /// (aspire#12036 tracks further speed-ups). Override with a licensed Oracle
    /// image as needed.
    /// </summary>
    [Required]
    public string Image { get; init; } = "gvenzl/oracle-free:23.5-slim-faststart";

    [Range(60, 900)]
    public int StartupTimeoutSeconds { get; init; } = 300;

    [Required]
    public string Username { get; init; } = "rigtunit";

    [Required]
    public string Password { get; init; } = "rigtunit";
}
