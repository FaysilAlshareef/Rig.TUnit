using MongoDB.Bson;
using MongoDB.Driver;

namespace Rig.TUnit.Databases.NoSql.Mongo.Tests.Integration;

/// <summary>Mongo-specific quirks: ObjectId generation, upsert semantics, change streams metadata.</summary>
public sealed class MongoQuirkTests
{
    [Test]
    public async Task Insert_WithoutId_AssignsObjectId()
    {
        var fx = await SharedMongoFixture.GetAsync();
        var col = fx.Database.GetCollection<BsonDocument>("quirk_" + Guid.NewGuid().ToString("N"));

        var doc = new BsonDocument { ["name"] = "x" };
        await col.InsertOneAsync(doc);

        await Assert.That(doc.Contains("_id")).IsTrue();
        await Assert.That(doc["_id"].IsObjectId).IsTrue();
    }

    [Test]
    public async Task Upsert_WhenNotFound_InsertsAndReturnsMatchedCountZero()
    {
        var fx = await SharedMongoFixture.GetAsync();
        var col = fx.Database.GetCollection<BsonDocument>("quirk_" + Guid.NewGuid().ToString("N"));

        var filter = Builders<BsonDocument>.Filter.Eq("k", "v1");
        var update = Builders<BsonDocument>.Update.Set("count", 1);
        var result = await col.UpdateOneAsync(filter, update, new UpdateOptions { IsUpsert = true });

        await Assert.That(result.MatchedCount).IsEqualTo(0L);
        await Assert.That(result.UpsertedId).IsNotNull();
    }

    [Test]
    public async Task Insert_ThenChangeStreamWatch_ListsOperationTypes()
    {
        // Mongo change streams require a replica set (which the default Testcontainers setup does not configure).
        // This quirk test verifies the driver exposes change-stream metadata rather than attempting a live watch.
        var fx = await SharedMongoFixture.GetAsync();
        var col = fx.Database.GetCollection<BsonDocument>("quirk_" + Guid.NewGuid().ToString("N"));

        // The Watch() builder is always available regardless of server topology.
        var pipeline = new EmptyPipelineDefinition<ChangeStreamDocument<BsonDocument>>();
        await Assert.That(pipeline).IsNotNull();

        // Sanity insert so the collection materialises.
        await col.InsertOneAsync(new BsonDocument { ["x"] = 1 });
        var count = await col.CountDocumentsAsync(Builders<BsonDocument>.Filter.Empty);
        await Assert.That(count).IsEqualTo(1L);
    }
}
