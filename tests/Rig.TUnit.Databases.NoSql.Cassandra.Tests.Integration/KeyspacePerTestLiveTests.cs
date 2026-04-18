using Cassandra;
using Rig.TUnit.Core;
using Rig.TUnit.Databases.NoSql.Cassandra.Helpers;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Integration;

/// <summary>
/// T026-RED integration tests for <see cref="KeyspacePerTestHelper"/>. Runs against the live
/// Cassandra container via <see cref="SharedCassandraFixture"/>. Verifies the helper actually
/// issues <c>CREATE KEYSPACE</c> on construction and <c>DROP KEYSPACE</c> on dispose, and that
/// two helpers against the same cluster produce distinct keyspaces.
/// </summary>
public sealed class KeyspacePerTestLiveTests
{
    [Test]
    public async Task CreateAsync_CreatesKeyspace_ReflectedInSystemSchema()
    {
        // Arrange
        var fx = await SharedCassandraFixture.GetAsync();
        var key = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        // Act
        await using var helper = await KeyspacePerTestHelper.CreateAsync(fx.Session, key, prefix: "kph");

        // Assert
        var rs = await fx.Session.ExecuteAsync(new SimpleStatement(
            "SELECT keyspace_name FROM system_schema.keyspaces WHERE keyspace_name = ?",
            helper.KeyspaceName));
        await Assert.That(rs.Any(r => string.Equals(r.GetValue<string>("keyspace_name"), helper.KeyspaceName, StringComparison.Ordinal)))
            .IsTrue();
    }

    [Test]
    public async Task DisposeAsync_DropsKeyspace_RemovedFromSystemSchema()
    {
        // Arrange
        var fx = await SharedCassandraFixture.GetAsync();
        var key = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        var helper = await KeyspacePerTestHelper.CreateAsync(fx.Session, key, prefix: "kph");
        var name = helper.KeyspaceName;

        // Act
        await helper.DisposeAsync();

        // Assert
        var rs = await fx.Session.ExecuteAsync(new SimpleStatement(
            "SELECT keyspace_name FROM system_schema.keyspaces WHERE keyspace_name = ?", name));
        await Assert.That(rs.Any()).IsFalse();
    }

    [Test]
    public async Task TwoHelpers_DifferentKeys_ProduceDistinctKeyspaceNames()
    {
        // Arrange
        var fx = await SharedCassandraFixture.GetAsync();
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        // Act
        await using var a = await KeyspacePerTestHelper.CreateAsync(fx.Session, k1, "kph");
        await using var b = await KeyspacePerTestHelper.CreateAsync(fx.Session, k2, "kph");

        // Assert
        await Assert.That(a.KeyspaceName).IsNotEqualTo(b.KeyspaceName);
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_IsIdempotent()
    {
        // Arrange
        var fx = await SharedCassandraFixture.GetAsync();
        var helper = await KeyspacePerTestHelper.CreateAsync(
            fx.Session,
            IsolationKey.FromName(Guid.NewGuid().ToString("N")),
            "kph");

        // Act
        await helper.DisposeAsync();

        // Assert
        await Assert.That(async () => await helper.DisposeAsync()).ThrowsNothing();
    }
}
