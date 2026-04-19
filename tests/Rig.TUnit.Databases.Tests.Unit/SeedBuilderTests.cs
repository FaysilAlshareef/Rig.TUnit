using Rig.TUnit.Databases.Seeding;

namespace Rig.TUnit.Databases.Tests.Unit;

public sealed class SeedBuilderTests
{
    public sealed record Customer(int Id, string Name);

    [Test]
    public async Task Add_AccumulatesRows()
    {
        var builder = new SeedBuilder<Customer>();

        builder.Add(new Customer(1, "a")).Add(new Customer(2, "b"));

        await Assert.That(builder.Build().Count).IsEqualTo(2);
    }

    [Test]
    public async Task Generate_ProducesRequestedCountViaBogus()
    {
        var builder = new SeedBuilder<Customer>();

        builder.Generate(5, f =>
        {
            var cursor = 0;
            f.CustomInstantiator(faker => new Customer(++cursor, faker.Name.FirstName()));
        });

        await Assert.That(builder.Build().Count).IsEqualTo(5);
    }

    [Test]
    public async Task DependsOn_RecordsDependency()
    {
        var builder = new SeedBuilder<Customer>();

        builder.DependsOn("Orders");

        await Assert.That(builder.Dependencies).Contains("Orders");
    }
}
