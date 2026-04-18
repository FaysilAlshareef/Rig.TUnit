namespace Rig.TUnit.Storage.AzureBlob.Tests.Unit;

internal sealed class TestClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
}
