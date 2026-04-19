using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Integration;

/// <summary>
/// T030-RED integration tests for <see cref="GsiVerifier"/> against live LocalStack-backed
/// DynamoDB. Creates a table with a known GSI, verifies VerifyAsync agrees; then asserts
/// the verifier detects injected mismatches (extra expected GSI, partition-key drift).
/// </summary>
public sealed class GsiVerifierLiveTests
{
    private static async Task<string> CreateTableWithGsiAsync(IAmazonDynamoDB client, string gsiName)
    {
        var tableName = $"gsiverifier_{Guid.NewGuid():N}";
        await client.CreateTableAsync(new CreateTableRequest
        {
            TableName = tableName,
            KeySchema = new List<KeySchemaElement>
            {
                new("Pk", KeyType.HASH),
            },
            AttributeDefinitions = new List<AttributeDefinition>
            {
                new("Pk", ScalarAttributeType.S),
                new("Email", ScalarAttributeType.S),
            },
            BillingMode = BillingMode.PAY_PER_REQUEST,
            GlobalSecondaryIndexes = new List<GlobalSecondaryIndex>
            {
                new()
                {
                    IndexName = gsiName,
                    KeySchema = new List<KeySchemaElement> { new("Email", KeyType.HASH) },
                    Projection = new Projection { ProjectionType = ProjectionType.ALL },
                },
            },
        });
        return tableName;
    }

    [Test]
    public async Task VerifyAsync_ExpectedGsiPresent_DoesNotThrow()
    {
        // Arrange
        var fx = await SharedDynamoFixture.GetAsync();
        var table = await CreateTableWithGsiAsync(fx.Client, "by-email");

        // Act + Assert
        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            fx.Client,
            table,
            new[] { new GsiExpectation("by-email", "Email") }))
            .ThrowsNothing();
    }

    [Test]
    public async Task VerifyAsync_ExpectedGsiMissing_Throws()
    {
        // Arrange
        var fx = await SharedDynamoFixture.GetAsync();
        var table = await CreateTableWithGsiAsync(fx.Client, "by-email");

        // Act + Assert
        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            fx.Client,
            table,
            new[] { new GsiExpectation("by-sku", "Sku") }))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task VerifyAsync_PartitionKeyDrift_Throws()
    {
        // Arrange
        var fx = await SharedDynamoFixture.GetAsync();
        var table = await CreateTableWithGsiAsync(fx.Client, "by-email");

        // Act + Assert
        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            fx.Client,
            table,
            new[] { new GsiExpectation("by-email", "UserId") }))
            .ThrowsExactly<InvalidOperationException>();
    }
}
