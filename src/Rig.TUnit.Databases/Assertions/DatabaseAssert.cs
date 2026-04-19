using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Assertions;

/// <summary>
/// Provider-agnostic database assertions. Each provider supplies its own
/// <c>ISchemaProbe</c> that these static helpers delegate to.
/// </summary>
public static class DatabaseAssert
{
    /// <summary>Asserts that a table (or collection) with the given name exists in the rig's database.</summary>
    public static Task TableExists(IDbRig rig, ISchemaProbe probe, string tableName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.TableExistsAsync(rig, tableName, ct);
    }

    /// <summary>Asserts that a table's row count equals <paramref name="expected"/>.</summary>
    public static Task RowCount(IDbRig rig, ISchemaProbe probe, string tableName, long expected, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.RowCountAsync(rig, tableName, expected, ct);
    }

    /// <summary>Asserts that a column's declared SQL type matches <paramref name="expectedType"/>.</summary>
    public static Task ColumnType(IDbRig rig, ISchemaProbe probe, string tableName, string columnName, string expectedType, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.ColumnTypeAsync(rig, tableName, columnName, expectedType, ct);
    }

    /// <summary>Asserts that an index with the given name exists on the specified table.</summary>
    public static Task IndexExists(IDbRig rig, ISchemaProbe probe, string tableName, string indexName, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(probe);
        return probe.IndexExistsAsync(rig, tableName, indexName, ct);
    }
}

/// <summary>Provider-specific schema inspection contract used by <see cref="DatabaseAssert"/>.</summary>
public interface ISchemaProbe
{
    Task TableExistsAsync(IDbRig rig, string tableName, CancellationToken ct);
    Task RowCountAsync(IDbRig rig, string tableName, long expected, CancellationToken ct);
    Task ColumnTypeAsync(IDbRig rig, string tableName, string columnName, string expectedType, CancellationToken ct);
    Task IndexExistsAsync(IDbRig rig, string tableName, string indexName, CancellationToken ct);
}
