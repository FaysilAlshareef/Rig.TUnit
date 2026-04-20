using Grpc.Net.Client;
using Rig.TUnit.Grpc.Helpers;

namespace Rig.TUnit.Grpc.Tests.Contract;

/// <summary>
/// Base contract every gRPC-backed rig must satisfy. Provider suites derive via
/// <c>[InheritsTests]</c> and may override <see cref="BaseAddress"/> to point at a
/// real test server.
/// </summary>
public abstract class GrpcRigContract
{
    protected virtual string BaseAddress => "http://localhost:5000";

    [Test]
    public async Task Channel_CanBeConstructedForBaseAddress()
    {
        using var channel = GrpcChannel.ForAddress(BaseAddress);

        await Assert.That(channel).IsNotNull();
    }

    [Test]
    public async Task MetadataHelper_ProducesBinaryHeaderWithProvidedClaims()
    {
        var md = MetadataHelper.Build(new Dictionary<string, string> { ["tenant"] = "acme" });

        await Assert.That(md.Count).IsEqualTo(1);
        await Assert.That(md[0].IsBinary).IsTrue();
    }
}
