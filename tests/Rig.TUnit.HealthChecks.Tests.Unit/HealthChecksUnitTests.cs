namespace Rig.TUnit.HealthChecks.Tests.Unit;

public sealed class HealthChecksUnitTests
{
    [Test]
    public async Task T035_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T035 populates this test.");
    }
}
