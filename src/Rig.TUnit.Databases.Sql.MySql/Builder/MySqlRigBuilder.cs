using Microsoft.EntityFrameworkCore;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Sql.Builder;

namespace Rig.TUnit.Databases.Sql.MySql.Builder;

public sealed class MySqlRigBuilder : SqlRigBuilder<MySqlRigBuilder>
{
    public MySqlRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source) { }

    /// <summary>
    /// EF Core integration is consumer-provided — Pomelo.EntityFrameworkCore.MySql 9.x pins
    /// EF Relational &lt;= 9.0.999 and is not yet available for EF Core 10 (as of 2026-04;
    /// Pomelo PR #2019 tracks the uplift). To wire a DbContext against this fixture,
    /// install <c>Pomelo.EntityFrameworkCore.MySql</c> in your test project and call
    /// <c>options.UseMySql(fixture.ConnectionString, ServerVersion.AutoDetect(...))</c> yourself.
    /// The Fixture still produces a working MySQL container and raw connection string.
    /// </summary>
    protected override void UseProvider(DbContextOptionsBuilder options, string connectionString)
    {
        throw new NotSupportedException(
            "MySql EF Core 10 integration is consumer-provided. Install Pomelo.EntityFrameworkCore.MySql "
            + "(or the Pomelo EF10 preview) in your test project and call options.UseMySql(connectionString, "
            + "ServerVersion.AutoDetect(connectionString)) directly. See Rig.TUnit.Databases.Sql.MySql README.");
    }
}
