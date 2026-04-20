namespace Rig.TUnit.Parallelism.Tests.Unit;

public sealed class ParallelismUnitTests
{
    [Test]
    public async Task T037_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T037 populates this test.");
    }
}
