namespace Rig.TUnit.Observability.Tracing.Tests.Unit;

public sealed class TracingUnitTests
{
    [Test]
    public async Task Placeholder_WithUnimplementedBaseline_ThrowsInvalidOperation()
    {
        // RED sentinel. T055 replaces with real tests.
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T055 populates this test.");
    }
}
