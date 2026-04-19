using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;
using Rig.TUnit.Security.Jwt;
using Rig.TUnit.Security.Jwt.Options;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
[Config(typeof(InProcessEmitBenchmarkConfig))]
public class JwtBenchmarks
{
    private static readonly byte[] Hs256Key = RandomNumberGenerator.GetBytes(32);

    [Benchmark]
    public JwtBuilderOptions Options_ConstructWithDefaults() => new() { DefaultIssuer = "iss", DefaultAudience = "aud" };

    [Benchmark]
    public string Builder_SignHs256()
        => JwtBuilder.Create()
            .Issuer("iss")
            .Audience("aud")
            .Subject("user-1")
            .ExpiresIn(TimeSpan.FromMinutes(5))
            .SignedWithHs256(Hs256Key)
            .Build();
}
