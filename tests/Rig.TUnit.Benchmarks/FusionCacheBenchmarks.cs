using BenchmarkDotNet.Attributes;
using Rig.TUnit.Caching.Fusion.Helpers;
using Rig.TUnit.Caching.Fusion.Options;
using ZiggyCreatures.Caching.Fusion;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// FR-035 allocation benchmarks for FusionCache Options + pure-function helpers.
/// No cache + no container — helpers run entirely in-memory. Uses
/// <see cref="InProcessEmitBenchmarkConfig"/> to avoid BDN's external build timeout.
/// </summary>
[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class FusionCacheBenchmarks
{
    private static readonly FusionCacheEntryOptions EntryOptions = new()
    {
        Duration = TimeSpan.FromMinutes(10),
        IsFailSafeEnabled = true,
        FailSafeMaxDuration = TimeSpan.FromHours(1),
        EagerRefreshThreshold = 0.8f,
    };

    [Benchmark]
    public FusionCacheFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public FusionCacheFixtureOptions Options_ConstructWithOverrides() => new()
    {
        DefaultDurationSeconds = 120,
        IsFailSafeEnabled = false,
        FailSafeMaxDurationSeconds = 7200,
        EagerRefreshThreshold = 0.5f,
    };

    [Benchmark]
    public bool FailSafeHelper_IsApplicable()
        => FailSafeHelper.IsFailSafeApplicable(EntryOptions, TimeSpan.FromMinutes(30));

    [Benchmark]
    public bool EagerRefreshHelper_ShouldRefresh()
        => EagerRefreshHelper.ShouldEagerRefresh(EntryOptions, TimeSpan.FromMinutes(9));
}
