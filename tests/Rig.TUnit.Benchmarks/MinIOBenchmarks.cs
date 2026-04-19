using BenchmarkDotNet.Attributes;
using Rig.TUnit.Storage.MinIO.Helpers;
using Rig.TUnit.Storage.MinIO.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class MinIOBenchmarks
{
    [Benchmark]
    public MinIOFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public MinIOFixtureOptions Options_ConstructWithOverrides() => new() { ImageTag = "RELEASE.2025-01-20T14-49-07Z", StartupTimeoutSeconds = 120, Username = "admin", Password = "secret" };

    [Benchmark]
    public MinIOPresignRequest SasBuilder_BuildPresignRequest()
        => MinIOSasBuilder.BuildPresignRequest("bucket", "key", "GET", TimeSpan.FromMinutes(5), TimeProvider.System);
}
