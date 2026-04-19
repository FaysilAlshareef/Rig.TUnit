using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;
using BenchmarkDotNet.Attributes;
using NSubstitute;
using Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// T030-RED benchmark for <see cref="GsiVerifier.VerifyAsync"/>. Uses an NSubstitute
/// mock <see cref="IAmazonDynamoDB"/> so no container / network is involved — measures
/// allocation cost of the verification loop itself.
/// </summary>
[MemoryDiagnoser]
public class DynamoBenchmarks
{
    private IAmazonDynamoDB _client = null!;
    private GsiExpectation[] _oneExpected = null!;
    private GsiExpectation[] _fiveExpected = null!;

    [GlobalSetup]
    public void Setup()
    {
        _client = Substitute.For<IAmazonDynamoDB>();

        var tableResponse = new DescribeTableResponse
        {
            Table = new TableDescription
            {
                TableName = "bench",
                GlobalSecondaryIndexes = new List<GlobalSecondaryIndexDescription>
                {
                    Make("by-email", "Email"),
                    Make("by-sku", "Sku", "CreatedAt"),
                    Make("by-tenant", "TenantId"),
                    Make("by-org", "OrgId"),
                    Make("by-region", "Region"),
                },
            },
        };
        _client.DescribeTableAsync("bench", Arg.Any<CancellationToken>()).Returns(tableResponse);

        _oneExpected = new[] { new GsiExpectation("by-email", "Email") };
        _fiveExpected = new[]
        {
            new GsiExpectation("by-email", "Email"),
            new GsiExpectation("by-sku", "Sku", "CreatedAt"),
            new GsiExpectation("by-tenant", "TenantId"),
            new GsiExpectation("by-org", "OrgId"),
            new GsiExpectation("by-region", "Region"),
        };
    }

    private static GlobalSecondaryIndexDescription Make(string name, string pk, string? sk = null)
    {
        var schema = new List<KeySchemaElement> { new(pk, KeyType.HASH) };
        if (sk is not null) schema.Add(new KeySchemaElement(sk, KeyType.RANGE));
        return new GlobalSecondaryIndexDescription
        {
            IndexName = name,
            KeySchema = schema,
            IndexStatus = "ACTIVE",
        };
    }

    [Benchmark]
    public Task Verify_OneGsi() => GsiVerifier.VerifyAsync(_client, "bench", _oneExpected);

    [Benchmark]
    public Task Verify_FiveGsis() => GsiVerifier.VerifyAsync(_client, "bench", _fiveExpected);
}
