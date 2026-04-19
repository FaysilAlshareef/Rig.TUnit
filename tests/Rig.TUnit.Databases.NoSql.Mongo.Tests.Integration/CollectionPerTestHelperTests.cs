using MongoDB.Bson;
using MongoDB.Driver;
using Rig.TUnit.Core;
using Rig.TUnit.Databases.NoSql.Mongo.Helpers;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration;

/// <summary>
/// T022-RED integration test for <see cref="CollectionPerTestHelper"/>. Runs against the
/// live Mongo container via <see cref="SharedMongoFixture"/>. Verifies per-test collection
/// isolation — two helpers produce distinct collections — and that <c>DisposeAsync</c>
/// drops the test's collection.
/// </summary>
public sealed class CollectionPerTestHelperTests
{
    [Test]
    public async Task GetCollection_WithIsolationKey_ReturnsCollectionUsingKeyName()
    {
        // Arrange
        var fx = await SharedMongoFixture.GetAsync();
        var key = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        // Act
        await using var helper = new CollectionPerTestHelper(fx.Database, key, prefix: "cpt");

        // Assert
        await Assert.That(helper.CollectionName).Contains(key.Value);
    }

    [Test]
    public async Task TwoHelpers_SameDatabaseDifferentKeys_ProduceDistinctCollectionNames()
    {
        // Arrange
        var fx = await SharedMongoFixture.GetAsync();
        var k1 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        var k2 = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        // Act
        await using var helperA = new CollectionPerTestHelper(fx.Database, k1);
        await using var helperB = new CollectionPerTestHelper(fx.Database, k2);

        // Assert
        await Assert.That(helperA.CollectionName).IsNotEqualTo(helperB.CollectionName);
    }

    [Test]
    public async Task DisposeAsync_CalledTwice_IsIdempotent()
    {
        // Arrange
        var fx = await SharedMongoFixture.GetAsync();
        var helper = new CollectionPerTestHelper(
            fx.Database,
            IsolationKey.FromName(Guid.NewGuid().ToString("N")));

        // Act
        await helper.DisposeAsync();

        // Assert
        await Assert.That(async () => await helper.DisposeAsync()).ThrowsNothing();
    }

    [Test]
    public async Task DisposeAsync_DropsCollection_ReflectedInDatabaseListing()
    {
        // Arrange
        var fx = await SharedMongoFixture.GetAsync();
        var key = IsolationKey.FromName(Guid.NewGuid().ToString("N"));
        var helper = new CollectionPerTestHelper(fx.Database, key);
        var collection = helper.GetCollection<BsonDocument>();
        await collection.InsertOneAsync(new BsonDocument { ["probe"] = 1 });

        var namesBefore = await (await fx.Database.ListCollectionNamesAsync()).ToListAsync();
        await Assert.That(namesBefore).Contains(helper.CollectionName);

        // Act
        await helper.DisposeAsync();

        // Assert
        var namesAfter = await (await fx.Database.ListCollectionNamesAsync()).ToListAsync();
        await Assert.That(namesAfter).DoesNotContain(helper.CollectionName);
    }

    [Test]
    public async Task Ctor_NullDatabase_Throws()
    {
        // Arrange
        var key = IsolationKey.FromName(Guid.NewGuid().ToString("N"));

        // Act + Assert
        await Assert.That(() => new CollectionPerTestHelper(null!, key))
            .ThrowsExactly<ArgumentNullException>();
    }
}
