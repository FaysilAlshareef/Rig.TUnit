namespace Rig.TUnit.Resilience.Tests.Unit;

public sealed class ResilienceUnitTests
{
    [Test]
    public async Task Sentinel_BeforeT039Populates_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T039 populates this test.");
    }
}
