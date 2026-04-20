namespace Rig.TUnit.Core.Tests.Contract;

/// <summary>
/// Feature 005 T020 RED sentinel — replaced by T021 GREEN with the actual
/// base contract every Core-level rig implementation must satisfy.
/// </summary>
public abstract class CoreRigContract
{
    [Test]
    public async Task T021_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T021 populates this contract.");
    }
}
