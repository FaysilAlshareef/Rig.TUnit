namespace Rig.TUnit.Core.Builder;

internal sealed class ValueConnectionSource(string connectionString) : IRigConnectionSource
{
    public string ConnectionString { get; } = connectionString
        ?? throw new ArgumentNullException(nameof(connectionString));
}
