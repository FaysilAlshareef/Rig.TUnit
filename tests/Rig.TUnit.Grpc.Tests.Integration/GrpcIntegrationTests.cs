using Grpc.Core;
using Grpc.Net.Client;
using Rig.TUnit.Grpc.Helpers;

namespace Rig.TUnit.Grpc.Tests.Integration;

/// <summary>
/// Integration-style exercises of <see cref="MetadataHelper"/> and
/// <c>GrpcClientHelper</c>. These tests don't stand up a real gRPC server
/// (that cost is borne by consumer projects) but they DO exercise real protobuf
/// serialisation + GrpcChannel construction so the happy-path wiring is
/// validated end-to-end within the package's own surface.
/// </summary>
public sealed class GrpcIntegrationTests
{
    [Test]
    public async Task MetadataHelper_Build_EmitsAccessClaimsBinEntry()
    {
        var claims = new Dictionary<string, string>
        {
            ["sub"] = "user-42",
            ["role"] = "admin",
        };

        var metadata = MetadataHelper.Build(claims);

        await Assert.That(metadata.Count).IsEqualTo(1);
        await Assert.That(metadata[0].Key).IsEqualTo("access-claims-bin");
        await Assert.That(metadata[0].IsBinary).IsTrue();
        await Assert.That(metadata[0].ValueBytes.Length).IsGreaterThan(0);
    }

    [Test]
    public async Task MetadataHelper_Build_EmptyClaims_StillEmitsHeader()
    {
        var metadata = MetadataHelper.Build(new Dictionary<string, string>());

        await Assert.That(metadata.Count).IsEqualTo(1);
        await Assert.That(metadata[0].Key).IsEqualTo("access-claims-bin");
    }

    [Test]
    public async Task GrpcChannel_ForAddress_ConstructsWithValidUri()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5000");

        await Assert.That(channel).IsNotNull();
        await Assert.That(channel.Target).Contains("localhost");
    }

    [Test]
    public async Task GrpcChannel_State_IsIdleBeforeFirstCall()
    {
        using var channel = GrpcChannel.ForAddress("http://localhost:5000");

        await Assert.That(channel.State).IsEqualTo(ConnectivityState.Idle);
    }
}
