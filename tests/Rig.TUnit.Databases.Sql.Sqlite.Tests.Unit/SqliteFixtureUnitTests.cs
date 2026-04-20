namespace Rig.TUnit.Databases.Sql.Sqlite.Tests.Unit;

public sealed class SqliteFixtureUnitTests
{
    [Test]
    public async Task Sentinel_BeforeT045Populates_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T045 populates this test.");
    }
}
