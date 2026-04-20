namespace Rig.TUnit.Databases.NoSql.Redis.Tests.Unit;

public sealed class RedisKvUnitTests
{
    [Test]
    public async Task Sentinel_BeforeT049Populates_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T049 populates this test.");
    }
}
