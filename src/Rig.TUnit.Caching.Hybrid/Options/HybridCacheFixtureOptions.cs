using System.ComponentModel.DataAnnotations;

namespace Rig.TUnit.Caching.Hybrid.Options;

/// <summary>
/// Configuration for <see cref="Fixtures.HybridCacheFixture"/>. HybridCache is an
/// in-process L1 cache — no container is started. These options govern default entry
/// expiration, local-cache expiration, and the payload/key size guards exposed by
/// <c>Microsoft.Extensions.Caching.Hybrid.HybridCacheOptions</c>.
/// </summary>
public sealed class HybridCacheFixtureOptions
{
    public const string SectionName = "RigTUnit:HybridCache";

    [Range(1, 86400)]
    public int DefaultExpirationSeconds { get; init; } = 60;

    [Range(1, 86400)]
    public int LocalCacheExpirationSeconds { get; init; } = 30;

    [Range(1, int.MaxValue)]
    public int MaximumPayloadBytes { get; init; } = 1024 * 1024;

    [Range(1, int.MaxValue)]
    public int MaximumKeyLength { get; init; } = 1024;
}
