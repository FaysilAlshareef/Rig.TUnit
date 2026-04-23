namespace Rig.TUnit.Microservices.Outbox.Tests.Unit;

public sealed class OutboxSchemaTests
{
    // ─── QualifiedTable ───────────────────────────────────────────────────────

    [Test]
    public async Task OutboxSchema_DefaultValues_QualifiedTableIsDboOutbox()
    {
        // Arrange + Act
        var schema = new OutboxSchema();

        // Assert
        await Assert.That(schema.QualifiedTable).IsEqualTo("[dbo].[Outbox]");
    }

    [Test]
    public async Task OutboxSchema_CustomSchemaAndTable_QualifiedTableUsesThoseValues()
    {
        // Arrange + Act
        var schema = new OutboxSchema(TableName: "DomainEvents", SchemaName: "evt");

        // Assert
        await Assert.That(schema.QualifiedTable).IsEqualTo("[evt].[DomainEvents]");
    }

    // ─── SQL builders ─────────────────────────────────────────────────────────

    [Test]
    public async Task OutboxSchema_BuildReadPendingSql_ContainsQualifiedTableAndBatchSize()
    {
        // Arrange
        var schema = new OutboxSchema();

        // Act
        var sql = schema.BuildReadPendingSql(batchSize: 25);

        // Assert
        await Assert.That(sql).Contains("[dbo].[Outbox]");
        await Assert.That(sql).Contains("25");
    }

    [Test]
    public async Task OutboxSchema_BuildInsertSql_ContainsQualifiedTableAndParameters()
    {
        // Arrange
        var schema = new OutboxSchema();

        // Act
        var sql = schema.BuildInsertSql();

        // Assert
        await Assert.That(sql).Contains("[dbo].[Outbox]");
        await Assert.That(sql).Contains("@id");
        await Assert.That(sql).Contains("@aggId");
    }

    [Test]
    public async Task OutboxSchema_BuildMarkRelayedSql_ContainsRelayedAtAndIdParameters()
    {
        // Arrange
        var schema = new OutboxSchema();

        // Act
        var sql = schema.BuildMarkRelayedSql();

        // Assert
        await Assert.That(sql).Contains("[dbo].[Outbox]");
        await Assert.That(sql).Contains("@relayedAt");
        await Assert.That(sql).Contains("@id");
    }

    [Test]
    public async Task OutboxSchema_BuildMarkFailedSql_ContainsReasonAndIdParameters()
    {
        // Arrange
        var schema = new OutboxSchema();

        // Act
        var sql = schema.BuildMarkFailedSql();

        // Assert
        await Assert.That(sql).Contains("[dbo].[Outbox]");
        await Assert.That(sql).Contains("@reason");
        await Assert.That(sql).Contains("@id");
    }

    [Test]
    public async Task OutboxSchema_CustomParameterPrefix_AppliedToInsertSql()
    {
        // Arrange
        var schema = new OutboxSchema(ParameterPrefix: ":");

        // Act
        var sql = schema.BuildInsertSql();

        // Assert
        await Assert.That(sql).Contains(":id");
        await Assert.That(sql).Contains(":aggId");
    }
}
