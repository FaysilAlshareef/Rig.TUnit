using Rig.TUnit.Databases.Fixtures;
using Rig.TUnit.Databases.NoSql.Contracts;

namespace Rig.TUnit.Databases.NoSql.Fixtures;

/// <summary>Abstract base for every document/NoSQL fixture.</summary>
public abstract class DocumentFixtureBase : DbFixtureBase, INoSqlRig
{
}
