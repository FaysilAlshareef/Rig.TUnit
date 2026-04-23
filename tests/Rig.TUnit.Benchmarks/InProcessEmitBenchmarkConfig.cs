using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Engines;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Toolchains.InProcess.Emit;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Smoke-benchmark config that uses <see cref="InProcessEmitToolchain"/> to avoid
/// BDN's external auto-generated boilerplate build (which exceeds the default
/// 2-minute timeout on our 100+ project transitive graph).
/// </summary>
public sealed class InProcessEmitBenchmarkConfig : ManualConfig
{
    public InProcessEmitBenchmarkConfig()
    {
        AddJob(Job.Dry
            .WithRuntime(BenchmarkDotNet.Environments.CoreRuntime.Core90)
            .WithToolchain(InProcessEmitToolchain.Instance)
            .WithStrategy(RunStrategy.ColdStart)
            .WithIterationCount(1)
            .WithWarmupCount(1)
            .WithLaunchCount(1)
            .WithInvocationCount(1)
            .WithUnrollFactor(1));
    }
}
