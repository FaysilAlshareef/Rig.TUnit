using Bogus;

namespace Rig.TUnit.Databases.Seeding;

/// <summary>
/// Collects seed rows for a single entity type with dependency-ordered emission.
/// Provider-specific adapters consume <see cref="Build"/> and perform the insert.
/// </summary>
/// <typeparam name="T">The entity type.</typeparam>
public sealed class SeedBuilder<T> where T : class
{
    private readonly List<T> _rows = [];
    private readonly List<string> _dependsOn = [];

    public int Count => _rows.Count;

    public IReadOnlyList<string> Dependencies => _dependsOn;

    public SeedBuilder<T> Add(T row)
    {
        ArgumentNullException.ThrowIfNull(row);
        _rows.Add(row);
        return this;
    }

    public SeedBuilder<T> AddRange(IEnumerable<T> rows)
    {
        ArgumentNullException.ThrowIfNull(rows);
        _rows.AddRange(rows);
        return this;
    }

    /// <summary>Generate <paramref name="count"/> rows via a Bogus <see cref="Faker{T}"/> configuration.</summary>
    public SeedBuilder<T> Generate(int count, Action<Faker<T>> configure)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);
        ArgumentNullException.ThrowIfNull(configure);

        var faker = new Faker<T>();
        configure(faker);
        _rows.AddRange(faker.Generate(count));
        return this;
    }

    /// <summary>Declares that this seed must be applied AFTER the named dependency seeds.</summary>
    public SeedBuilder<T> DependsOn(string dependencyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(dependencyName);
        _dependsOn.Add(dependencyName);
        return this;
    }

    public IReadOnlyList<T> Build() => _rows;
}
