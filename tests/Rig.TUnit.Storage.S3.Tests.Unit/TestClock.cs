namespace Rig.TUnit.Storage.S3.Tests.Unit;

internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
