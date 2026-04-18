using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using NSubstitute;
using Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

/// <summary>
/// T030-RED unit tests for <see cref="GsiVerifier"/> — asserts the verifier flags
/// GSI mismatches (missing, wrong partition key, wrong sort key, wrong status)
/// without touching a live DynamoDB / LocalStack. <see cref="IAmazonDynamoDB"/>
/// is replaced with an NSubstitute mock returning crafted DescribeTableResponses.
/// </summary>
public sealed class GsiVerifierTests
{
    private const string TableName = "orders";

    private static DescribeTableResponse BuildResponse(params GlobalSecondaryIndexDescription[] gsis)
    {
        var response = new DescribeTableResponse
        {
            Table = new TableDescription
            {
                TableName = TableName,
                GlobalSecondaryIndexes = gsis.ToList(),
            },
        };
        return response;
    }

    private static GlobalSecondaryIndexDescription MakeGsi(
        string name,
        string partitionKey,
        string? sortKey = null,
        string status = "ACTIVE")
    {
        var keySchema = new List<KeySchemaElement>
        {
            new() { AttributeName = partitionKey, KeyType = KeyType.HASH },
        };
        if (sortKey is not null)
        {
            keySchema.Add(new KeySchemaElement { AttributeName = sortKey, KeyType = KeyType.RANGE });
        }
        return new GlobalSecondaryIndexDescription
        {
            IndexName = name,
            KeySchema = keySchema,
            IndexStatus = status,
        };
    }

    [Test]
    public async Task VerifyAsync_ExpectedGsiPresent_DoesNotThrow()
    {
        // Arrange
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(MakeGsi("by-email", "Email")));

        // Act + Assert
        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[] { new GsiExpectation("by-email", "Email") }))
            .ThrowsNothing();
    }

    [Test]
    public async Task VerifyAsync_GsiMissing_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(MakeGsi("by-email", "Email")));

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[] { new GsiExpectation("by-sku", "Sku") }))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task VerifyAsync_PartitionKeyMismatch_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(MakeGsi("by-email", "Email")));

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[] { new GsiExpectation("by-email", "UserId") }))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task VerifyAsync_SortKeyMismatch_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(MakeGsi("by-email", "Email", sortKey: "CreatedAt")));

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[] { new GsiExpectation("by-email", "Email", SortKey: "UpdatedAt") }))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task VerifyAsync_StatusMismatch_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(MakeGsi("by-email", "Email", status: "CREATING")));

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[] { new GsiExpectation("by-email", "Email", Status: "ACTIVE") }))
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task VerifyAsync_NullClient_Throws()
    {
        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            null!,
            TableName,
            new[] { new GsiExpectation("by-email", "Email") }))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task VerifyAsync_EmptyTableName_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            string.Empty,
            new[] { new GsiExpectation("by-email", "Email") }))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task VerifyAsync_NullExpected_Throws()
    {
        var client = Substitute.For<IAmazonDynamoDB>();

        await Assert.That(async () => await GsiVerifier.VerifyAsync(client, TableName, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task VerifyAsync_TableHasNoGsis_ExpectedEmpty_DoesNotThrow()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse());

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            Array.Empty<GsiExpectation>()))
            .ThrowsNothing();
    }

    [Test]
    public async Task VerifyAsync_MultipleGsis_AllMatch_DoesNotThrow()
    {
        var client = Substitute.For<IAmazonDynamoDB>();
        client.DescribeTableAsync(TableName, Arg.Any<CancellationToken>())
            .Returns(BuildResponse(
                MakeGsi("by-email", "Email"),
                MakeGsi("by-sku", "Sku", "CreatedAt")));

        await Assert.That(async () => await GsiVerifier.VerifyAsync(
            client,
            TableName,
            new[]
            {
                new GsiExpectation("by-email", "Email"),
                new GsiExpectation("by-sku", "Sku", "CreatedAt"),
            }))
            .ThrowsNothing();
    }
}
