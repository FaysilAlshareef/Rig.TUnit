using Rig.TUnit.Databases.Fixtures;
using Rig.TUnit.Databases.Sql.Contracts;

namespace Rig.TUnit.Databases.Sql.Fixtures;

/// <summary>Abstract base for every SQL fixture. Inherits <see cref="DbFixtureBase"/>.</summary>
public abstract class SqlFixtureBase : DbFixtureBase, ISqlRig
{
}
