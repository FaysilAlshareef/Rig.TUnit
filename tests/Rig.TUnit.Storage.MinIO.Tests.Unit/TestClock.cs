namespace Rig.TUnit.Storage.MinIO.Tests.Unit;

internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
