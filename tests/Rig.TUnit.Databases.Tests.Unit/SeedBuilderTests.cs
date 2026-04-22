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

    [Test]
    public async Task Add_NullRow_ThrowsArgumentNullException()
    {
        var builder = new SeedBuilder<Customer>();

        await Assert.That(() => builder.Add(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AddRange_AccumulatesRows()
    {
        var builder = new SeedBuilder<Customer>();
        var rows = new[] { new Customer(1, "a"), new Customer(2, "b"), new Customer(3, "c") };

        builder.AddRange(rows);

        await Assert.That(builder.Build().Count).IsEqualTo(3);
    }

    [Test]
    public async Task AddRange_NullRows_ThrowsArgumentNullException()
    {
        var builder = new SeedBuilder<Customer>();

        await Assert.That(() => builder.AddRange(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Generate_ZeroCount_ThrowsArgumentOutOfRangeException()
    {
        var builder = new SeedBuilder<Customer>();

        await Assert.That(() => builder.Generate(0, _ => { })).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Generate_NullConfigure_ThrowsArgumentNullException()
    {
        var builder = new SeedBuilder<Customer>();

        await Assert.That(() => builder.Generate(1, null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task DependsOn_NullOrWhiteSpace_ThrowsArgumentException()
    {
        var builder = new SeedBuilder<Customer>();

        await Assert.That(() => builder.DependsOn("  ")).ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Count_ReflectsAddedRows()
    {
        var builder = new SeedBuilder<Customer>();
        builder.Add(new Customer(1, "x"));
        builder.Add(new Customer(2, "y"));

        await Assert.That(builder.Count).IsEqualTo(2);
    }

    [Test]
    public async Task Build_ReturnsReadOnlyListOfAddedRows()
    {
        var builder = new SeedBuilder<Customer>();
        builder.Add(new Customer(99, "z"));

        var result = builder.Build();

        await Assert.That(result).IsNotNull();
        await Assert.That(result.Count).IsEqualTo(1);
        await Assert.That(result[0].Id).IsEqualTo(99);
    }
}
