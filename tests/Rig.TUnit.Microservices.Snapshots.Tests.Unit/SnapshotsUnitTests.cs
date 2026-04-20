namespace Rig.TUnit.Microservices.Snapshots.Tests.Unit;

public sealed class SnapshotsUnitTests
{
    [Test]
    public async Task Placeholder_WithUnimplementedBaseline_ThrowsInvalidOperation()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T065 populates this test.");
    }
}
