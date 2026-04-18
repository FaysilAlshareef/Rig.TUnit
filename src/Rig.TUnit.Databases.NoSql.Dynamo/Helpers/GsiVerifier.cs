using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

/// <summary>
/// Verifies a table's Global Secondary Indexes match a declarative set of
/// <see cref="GsiExpectation"/>s. Throws <see cref="InvalidOperationException"/>
/// on the first mismatch — missing index, wrong partition key, wrong sort key,
/// or wrong status. Typical use in integration tests:
/// <code>
/// await GsiVerifier.VerifyAsync(client, "orders", new[]
/// {
///     new GsiExpectation("by-email", "Email"),
///     new GsiExpectation("by-sku", "Sku", SortKey: "CreatedAt"),
/// }, ct);
/// </code>
/// </summary>
public static class GsiVerifier
{
    public static async Task VerifyAsync(
        IAmazonDynamoDB client,
        string tableName,
        IReadOnlyList<GsiExpectation> expected,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        if (string.IsNullOrEmpty(tableName))
        {
            throw new ArgumentException("tableName is required.", nameof(tableName));
        }
        ArgumentNullException.ThrowIfNull(expected);

        if (expected.Count == 0)
        {
            return;
        }

        var description = await client.DescribeTableAsync(tableName, ct).ConfigureAwait(false);
        var gsis = description.Table?.GlobalSecondaryIndexes
            ?? new List<GlobalSecondaryIndexDescription>();

        foreach (var e in expected)
        {
            var gsi = gsis.FirstOrDefault(g => string.Equals(g.IndexName, e.IndexName, StringComparison.Ordinal));
            if (gsi is null)
            {
                throw new InvalidOperationException(
                    $"Table '{tableName}' is missing GSI '{e.IndexName}'.");
            }

            var partition = gsi.KeySchema?.FirstOrDefault(k => k.KeyType == KeyType.HASH);
            if (partition is null || !string.Equals(partition.AttributeName, e.PartitionKey, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"GSI '{e.IndexName}' on '{tableName}' has partition key "
                    + $"'{partition?.AttributeName ?? "<none>"}', expected '{e.PartitionKey}'.");
            }

            if (e.SortKey is not null)
            {
                var sort = gsi.KeySchema?.FirstOrDefault(k => k.KeyType == KeyType.RANGE);
                if (sort is null || !string.Equals(sort.AttributeName, e.SortKey, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"GSI '{e.IndexName}' on '{tableName}' has sort key "
                        + $"'{sort?.AttributeName ?? "<none>"}', expected '{e.SortKey}'.");
                }
            }

            if (!string.Equals(gsi.IndexStatus?.Value, e.Status, StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"GSI '{e.IndexName}' on '{tableName}' has status "
                    + $"'{gsi.IndexStatus?.Value ?? "<none>"}', expected '{e.Status}'.");
            }
        }
    }
}
