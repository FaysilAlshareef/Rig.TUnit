using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;

namespace Rig.TUnit.Databases.Sql.Sqlite.Builder;

public sealed class SqliteRigBuilder : SqlRigBuilder<SqliteRigBuilder>
{
    public SqliteRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
    {
        options.UseSqlite(connectionString);
    }
}
