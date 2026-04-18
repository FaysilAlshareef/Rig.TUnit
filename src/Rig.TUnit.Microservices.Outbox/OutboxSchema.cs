namespace Rig.TUnit.Microservices.Outbox;

/// <summary>
/// Describes the shape of an outbox table/collection. Developers override the
/// defaults to match their existing persistence schema — no renames needed.
/// Also exposes parametrised SQL snippets for the common read/mark-relayed paths.
/// </summary>
public sealed record OutboxSchema(
    string TableName = "Outbox",
    string IdColumn = "Id",
    string AggregateIdColumn = "AggregateId",
    string EventTypeColumn = "EventType",
    string PayloadColumn = "Payload",
    string OccurredAtColumn = "OccurredAt",
    string CorrelationIdColumn = "CorrelationId",
    string CausationIdColumn = "CausationId",
    string TraceparentColumn = "Traceparent",
    string RelayedAtColumn = "RelayedAt",
    string FailureReasonColumn = "FailureReason",
    string DeliveryAttemptsColumn = "DeliveryAttempts",
    string SchemaName = "dbo",
    string ParameterPrefix = "@")
{
    /// <summary>The fully qualified table identifier (<c>[schema].[table]</c>).</summary>
    public string QualifiedTable => $"[{SchemaName}].[{TableName}]";

    /// <summary>Reads up to N unrelayed, unfailed rows ordered by <c>OccurredAt</c>.</summary>
    public string BuildReadPendingSql(int batchSize) => $"""
        SELECT TOP ({batchSize}) [{IdColumn}], [{AggregateIdColumn}], [{EventTypeColumn}],
                                 [{PayloadColumn}], [{OccurredAtColumn}], [{CorrelationIdColumn}],
                                 [{CausationIdColumn}], [{TraceparentColumn}], [{RelayedAtColumn}],
                                 [{FailureReasonColumn}], [{DeliveryAttemptsColumn}]
        FROM {QualifiedTable}
        WHERE [{RelayedAtColumn}] IS NULL AND [{FailureReasonColumn}] IS NULL
        ORDER BY [{OccurredAtColumn}];
        """;

    /// <summary>Inserts an outbox row — parameter names use <see cref="ParameterPrefix"/>.</summary>
    public string BuildInsertSql() => $"""
        INSERT INTO {QualifiedTable}
          ([{IdColumn}], [{AggregateIdColumn}], [{EventTypeColumn}], [{PayloadColumn}],
           [{OccurredAtColumn}], [{CorrelationIdColumn}], [{CausationIdColumn}], [{TraceparentColumn}])
        VALUES ({ParameterPrefix}id, {ParameterPrefix}aggId, {ParameterPrefix}eventType, {ParameterPrefix}payload,
                {ParameterPrefix}occurredAt, {ParameterPrefix}correlationId, {ParameterPrefix}causationId, {ParameterPrefix}traceparent);
        """;

    /// <summary>Claim-and-mark-relayed. Assumes unique <c>Id</c> so concurrent relays compete for the same row.</summary>
    public string BuildMarkRelayedSql() => $"""
        UPDATE {QualifiedTable}
        SET [{RelayedAtColumn}] = {ParameterPrefix}relayedAt
        WHERE [{IdColumn}] = {ParameterPrefix}id AND [{RelayedAtColumn}] IS NULL;
        """;

    /// <summary>Mark a row failed and increment <see cref="DeliveryAttemptsColumn"/>.</summary>
    public string BuildMarkFailedSql() => $"""
        UPDATE {QualifiedTable}
        SET [{FailureReasonColumn}] = {ParameterPrefix}reason,
            [{DeliveryAttemptsColumn}] = [{DeliveryAttemptsColumn}] + 1
        WHERE [{IdColumn}] = {ParameterPrefix}id;
        """;
}
