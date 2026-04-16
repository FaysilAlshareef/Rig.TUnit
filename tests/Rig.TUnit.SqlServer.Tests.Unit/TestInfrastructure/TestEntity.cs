namespace Rig.TUnit.SqlServer.Tests.Unit.TestInfrastructure;

internal sealed class TestEntity
{
    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    public static TestEntity Create(string name) => new() { Name = name };
}
