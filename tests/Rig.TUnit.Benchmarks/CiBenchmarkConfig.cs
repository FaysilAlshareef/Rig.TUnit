using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// CI-tuned BenchmarkDotNet config. Tighter than the default <c>--job short</c>
/// preset (1 warmup × 3 iterations × 1 launch) without ballooning to
/// <c>--job medium</c> (2 warmup × 10 iterations × 2 launches), which would push
/// the workflow over an hour for the 180+ benchmark suite.
///
/// Empirical baseline on shared GitHub-hosted runners (ubuntu-latest):
/// <c>--job short</c> produces ±50% per-bench variance across consecutive runs
/// on identical source code, which makes any ratio-based regression alert
/// (we use 120%) fire continuously on noise. Bumping to 3 warmup × 5 iterations
/// brings variance into the ~10–15% range — below the alert threshold so real
/// regressions stand out.
///
/// Wired in via <c>Program.cs</c>; the workflow no longer passes
/// <c>--job</c> on the CLI so this config is the source of truth.
/// </summary>
internal sealed class CiBenchmarkConfig : ManualConfig
{
    public CiBenchmarkConfig()
    {
        AddJob(Job.Default
            .WithWarmupCount(3)
            .WithIterationCount(5)
            .WithLaunchCount(1)
            .WithStrategy(RunStrategy.Throughput));
    }
}
