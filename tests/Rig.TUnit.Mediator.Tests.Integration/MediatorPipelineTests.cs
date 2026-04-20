namespace Rig.TUnit.Mediator.Tests.Integration;

/// <summary>
/// Feature 005 T022 RED sentinel — T023 populates with real pipeline-dispatch tests
/// exercising <see cref="Helpers.HandlerHelper"/> end-to-end through the Mediator source generator.
/// </summary>
public sealed class MediatorPipelineTests
{
    [Test]
    public async Task T023_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T023 populates this test.");
    }
}
