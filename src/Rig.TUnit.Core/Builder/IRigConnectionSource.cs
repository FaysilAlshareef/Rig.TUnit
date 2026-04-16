namespace Rig.TUnit.Core.Builder;

/// <summary>
/// Provides a connection string from any source — container fixture, configuration, or raw value.
/// </summary>
public interface IRigConnectionSource
{
    string ConnectionString { get; }
}
