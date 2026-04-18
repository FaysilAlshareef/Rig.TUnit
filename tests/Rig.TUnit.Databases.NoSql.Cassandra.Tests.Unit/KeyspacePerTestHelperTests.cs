using Rig.TUnit.Core;
using Rig.TUnit.Databases.NoSql.Cassandra.Helpers;

namespace Rig.TUnit.Databases.NoSql.Cassandra.Tests.Unit;

/// <summary>
/// T026-RED pure-function unit tests for <see cref="KeyspacePerTestHelper.BuildSafeKeyspace"/>.
/// Cassandra keyspace DDL is concatenated from the prefix and isolation value, so every
/// injection vector has to be rejected here. The regex whitelists <c>[a-z][a-z0-9_]*</c> —
/// any character outside that set is a validation failure. Integration tests cover the live
/// <c>CREATE KEYSPACE</c> + <c>DROP KEYSPACE</c> path.
/// </summary>
public sealed class KeyspacePerTestHelperTests
{
    [Test]
    public async Task BuildSafeKeyspace_LowercasePrefix_ReturnsValidIdentifier()
    {
        // Arrange
        var isolation = IsolationKey.FromName("abc123");

        // Act
        var name = KeyspacePerTestHelper.BuildSafeKeyspace("test", isolation);

        // Assert
        await Assert.That(name).StartsWith("test_");
        await Assert.That(char.IsLower(name[0])).IsTrue();
    }

    [Test]
    public async Task BuildSafeKeyspace_WithUnderscoresAndDigits_IsAccepted()
    {
        // Arrange
        var isolation = IsolationKey.FromName("run");

        // Act + Assert
        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("a_1", isolation)).ThrowsNothing();
    }

    [Test]
    public async Task BuildSafeKeyspace_MaxLength_IsUnder48Characters()
    {
        // Cassandra keyspace names MUST be ≤ 48 chars (CQL spec).
        var isolation = IsolationKey.FromName(new string('x', 200));

        // Act
        var name = KeyspacePerTestHelper.BuildSafeKeyspace("test", isolation);

        // Assert
        await Assert.That(name.Length).IsLessThanOrEqualTo(48);
    }

    [Test]
    public async Task BuildSafeKeyspace_PrefixWithSemicolon_Throws()
    {
        // Classic SQL-injection vector: "test; DROP KEYSPACE foo; --"
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("test;drop", isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_PrefixWithSpace_Throws()
    {
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("bad prefix", isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_PrefixStartingWithDigit_Throws()
    {
        // CQL identifiers must start with a letter.
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("1bad", isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_PrefixWithUppercase_Throws()
    {
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("BadPrefix", isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_EmptyPrefix_Throws()
    {
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace(string.Empty, isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_NullPrefix_Throws()
    {
        var isolation = IsolationKey.FromName("x");

        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace(null!, isolation))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_DefaultIsolationKey_Throws()
    {
        // IsolationKey is a readonly record struct; default(IsolationKey).Value is null —
        // helper MUST reject it rather than concatenating "test_" to a null segment.
        await Assert.That(() => KeyspacePerTestHelper.BuildSafeKeyspace("test", default))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task BuildSafeKeyspace_DifferentIsolationKeys_ProduceDistinctNames()
    {
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        var n1 = KeyspacePerTestHelper.BuildSafeKeyspace("test", k1);
        var n2 = KeyspacePerTestHelper.BuildSafeKeyspace("test", k2);

        await Assert.That(n1).IsNotEqualTo(n2);
    }
}
