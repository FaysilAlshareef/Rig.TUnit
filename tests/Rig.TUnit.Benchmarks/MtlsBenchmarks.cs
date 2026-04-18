using BenchmarkDotNet.Attributes;
using Rig.TUnit.Security.Mtls;
using Rig.TUnit.Security.Mtls.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class MtlsBenchmarks
{
    [Benchmark]
    public MtlsFixtureOptions Options_ConstructWithDefaults() => new();

    [Benchmark]
    public object CertificateAuthority_Create()
    {
        using var ca = MtlsCertificateBuilder.CreateCertificateAuthority();
        return ca.Thumbprint;
    }

    [Benchmark]
    public object Leaf_CreateFromCa()
    {
        using var ca = MtlsCertificateBuilder.CreateCertificateAuthority();
        using var leaf = MtlsCertificateBuilder.CreateLeaf(ca);
        return leaf.Thumbprint;
    }
}
