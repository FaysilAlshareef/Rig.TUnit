using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Contracts;

/// <summary>
/// Contract implemented by every database fixture — SQL and NoSQL alike.
/// Exposes a connection string, the isolated database name, and the isolation key.
/// </summary>
public interface IDbRig : IRigConnectionSource
{
    /// <summary>Per-test isolated database, collection, keyspace, or container name.</summary>
    string DatabaseName { get; }

    /// <summary>The isolation key that composed this database's name.</summary>
    IsolationKey IsolationKey { get; }
}
