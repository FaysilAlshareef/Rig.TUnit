using Google.Protobuf;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Builds gRPC Metadata with access-claims-bin header containing protobuf-serialized claims.
/// </summary>
public static class MetadataHelper
{
    /// <summary>
    /// Builds gRPC metadata with claims serialized as protobuf binary in the access-claims-bin key.
    /// </summary>
    public static Metadata Build(Dictionary<string, string> claims)
    {
        var structValue = new Struct();
        foreach (var (key, value) in claims)
        {
            structValue.Fields[key] = Value.ForString(value);
        }

        var bytes = structValue.ToByteArray();

        return [new Metadata.Entry("access-claims-bin", bytes)];
    }
}
