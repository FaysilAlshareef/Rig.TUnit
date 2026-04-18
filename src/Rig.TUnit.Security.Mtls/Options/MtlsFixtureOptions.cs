using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Security.Mtls.Options;

public sealed class MtlsFixtureOptions
{
    public const string SectionName = "RigTUnit:Mtls";

    [Required]
    public string CaSubject { get; init; } = "CN=rigtunit-test-ca";

    [Required]
    public string ClientSubject { get; init; } = "CN=rigtunit-client";

    [Required]
    public string ServerSubject { get; init; } = "CN=rigtunit-server";

    [Range(1, 3650)]
    public int ValidityDays { get; init; } = 365;
}
