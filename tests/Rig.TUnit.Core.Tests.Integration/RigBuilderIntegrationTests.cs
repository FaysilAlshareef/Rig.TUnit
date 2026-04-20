namespace Rig.TUnit.Core.Tests.Integration;

/// <summary>
/// Feature 005 T020 RED sentinel — populated by T021 GREEN with real
/// end-to-end tests exercising RigBuilder + RigConnect + IsolationKey.
/// </summary>
public sealed class RigBuilderIntegrationTests
{
    [Test]
    public async Task T021_Placeholder_FailsUntilImplemented()
    {
        await Task.Yield();
        throw new InvalidOperationException("RED: baseline not implemented — T021 populates this test.");
    }
}
