namespace Rig.TUnit.Http.Tests.Contract;

/// <summary>
/// Feature 005 T028 RED sentinel — T029 populates with HttpMock rig contract.
/// </summary>
public abstract class HttpMockRigContract
{
    [Test]
    public async Task T029_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T029 populates this contract.");
    }
}
