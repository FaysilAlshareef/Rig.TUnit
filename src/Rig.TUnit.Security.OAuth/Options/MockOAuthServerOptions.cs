using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Security.OAuth.Options;

public sealed class MockOAuthServerOptions
{
    public const string SectionName = "RigTUnit:OAuth";

    /// <summary>Port the in-process ASP.NET host listens on. Use 0 for dynamic allocation.</summary>
    [Range(0, 65535)]
    public int Port { get; init; } = 0;

    [Required]
    public required string Issuer { get; init; }

    public IReadOnlyList<string> SupportedFlows { get; init; } =
        new[] { "ClientCredentials", "AuthorizationCode", "Refresh" };

    [Range(60, 86400)]
    public int TokenLifetimeSeconds { get; init; } = 3600;
}
