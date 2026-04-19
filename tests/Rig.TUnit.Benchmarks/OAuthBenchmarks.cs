using BenchmarkDotNet.Attributes;
using Rig.TUnit.Security.OAuth.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class OAuthBenchmarks
{
    [Benchmark]
    public MockOAuthServerOptions Options_ConstructWithDefaults() => new() { Issuer = "https://host/" };

    [Benchmark]
    public MockOAuthServerOptions Options_ConstructWithOverrides() => new() { Issuer = "https://host/", Port = 5055, TokenLifetimeSeconds = 900 };
}
