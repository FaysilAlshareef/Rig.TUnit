namespace Rig.TUnit.WebAPI.Tests.Contract;

/// <summary>
/// Feature 005 T026 RED sentinel — T027 populates with the WebAPI rig contract.
/// </summary>
public abstract class WebApiRigContract
{
    [Test]
    public async Task T027_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T027 populates this contract.");
    }
}
