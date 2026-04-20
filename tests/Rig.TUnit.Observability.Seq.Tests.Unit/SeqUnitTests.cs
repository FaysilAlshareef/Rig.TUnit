namespace Rig.TUnit.Observability.Seq.Tests.Unit;

public sealed class SeqUnitTests
{
    [Test]
    public async Task Placeholder_WithUnimplementedBaseline_ThrowsInvalidOperation()
    {
        // RED sentinel. T053 replaces with real tests.
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T053 populates this test.");
    }
}
