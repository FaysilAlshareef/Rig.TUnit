namespace Rig.TUnit.Core.Tests.Unit.TestInfrastructure;

internal sealed class TestEntity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;
    public DateTime CreatedAt { get; private set; }
    private TestEntity() { }
}
