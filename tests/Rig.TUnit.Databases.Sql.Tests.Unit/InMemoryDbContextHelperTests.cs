using Rig.TUnit.Databases.Sql.Contracts;
using Rig.TUnit.Databases.Sql.Fixtures;
using Rig.TUnit.Databases.Sql.Tests.Contract;

namespace Rig.TUnit.Databases.Sql.Tests.Unit;

/// <summary>
/// InMemory fast-path binding for <see cref="DbContextHelperCrudContract{TFixture}"/>.
/// Closes the three-way parity chain: InMemory (this) / Sqlite / container-backed
/// providers all expose the same CRUD surface.
/// </summary>
[InheritsTests]
public sealed class InMemoryDbContextHelperTests : DbContextHelperCrudContract<InMemoryDbContextHelperTests.InMemoryFixture>
{
    protected override async ValueTask<InMemoryFixture> CreateFixtureAsync(CancellationToken ct)
    {
        var fixture = new InMemoryFixture();
        await fixture.InitializeAsync();
        return fixture;
    }

    /// <summary>
    /// Minimal in-process <see cref="ISqlRig"/>: no container, no files. The
    /// "connection string" is the synthetic EF Core InMemory database name keyed
    /// by the fixture's isolation key.
    /// </summary>
    public sealed class InMemoryFixture : SqlFixtureBase
    {
        private string? _connectionString;

        public override string ConnectionString => _connectionString
            ?? throw new InvalidOperationException("InitializeAsync must run first");

        public override string DatabaseName => $"inmemory_{IsolationKey.Value}";

        public override Task InitializeAsync()
        {
            _connectionString ??= $"InMemory:{DatabaseName}";
            return Task.CompletedTask;
        }

        public override ValueTask DisposeAsync()
        {
            _connectionString = null;
            return ValueTask.CompletedTask;
        }
    }
}
