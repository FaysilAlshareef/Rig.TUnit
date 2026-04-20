namespace Rig.TUnit.Caching.Redis.Tests.Unit;

public sealed class RedisCacheUnitTests
{
    [Test]
    public async Task Sentinel_BeforeT043Populates_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T043 populates this test.");
    }
}
