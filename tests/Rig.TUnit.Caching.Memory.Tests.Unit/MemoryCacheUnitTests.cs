namespace Rig.TUnit.Caching.Memory.Tests.Unit;

public sealed class MemoryCacheUnitTests
{
    [Test]
    public async Task Sentinel_BeforeT041Populates_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T041 populates this test.");
    }
}
