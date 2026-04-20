namespace Rig.TUnit.Grpc.Tests.Contract;

/// <summary>
/// Feature 005 T024 RED sentinel — T025 populates with the gRPC-harness contract.
/// </summary>
public abstract class GrpcRigContract
{
    [Test]
    public async Task T025_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T025 populates this contract.");
    }
}
