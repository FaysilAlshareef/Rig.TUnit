namespace Rig.TUnit.Mediator.Tests.Contract;

/// <summary>
/// Feature 005 T022 RED sentinel — T023 replaces with the Mediator-harness contract
/// that provider suites inherit to assert handler resolution invariants.
/// </summary>
public abstract class MediatorRigContract
{
    [Test]
    public async Task T023_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T023 populates this contract.");
    }
}
