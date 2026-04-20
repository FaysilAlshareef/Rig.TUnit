namespace Rig.TUnit.Concurrency.Tests.Contract;

/// <summary>
/// Feature 005 T032 RED sentinel — T033 populates with concurrency-rig contract.
/// </summary>
public abstract class ConcurrencyRigContract
{
    [Test]
    public async Task T033_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T033 populates this contract.");
    }
}
