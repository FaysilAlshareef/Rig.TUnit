namespace Rig.TUnit.Databases.NoSql.Dynamo.Helpers;

/// <summary>
/// Declarative expectation for a DynamoDB Global Secondary Index (GSI) used with
/// <see cref="GsiVerifier.VerifyAsync"/>. Defaults <see cref="Status"/> to "ACTIVE".
/// </summary>
public sealed record GsiExpectation(
    string IndexName,
    string PartitionKey,
    string? SortKey = null,
    string Status = "ACTIVE");
