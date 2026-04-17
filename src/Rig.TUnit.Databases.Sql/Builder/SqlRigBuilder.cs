using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Databases.Builder;

namespace Rig.TUnit.Databases.Sql.Builder;

/// <summary>
/// Base builder for every SQL provider. Promotes <c>ReplaceDbContext&lt;TContext&gt;</c>
/// from the old per-provider implementation so Sqlite, Postgres, MySql, and Oracle
/// inherit the method without reimplementing it.
/// </summary>
/// <typeparam name="TSelf">Concrete builder type.</typeparam>
public abstract class SqlRigBuilder<TSelf> : DatabaseRigBuilder<TSelf>
    where TSelf : SqlRigBuilder<TSelf>
{
    protected SqlRigBuilder(RigBuilder root, IRigConnectionSource source) : base(root, source)
    {
    }

    /// <summary>
    /// Replaces any registration of <typeparamref name="TContext"/> with one bound to the rig's
    /// connection string (provider-specific binding happens in <see cref="UseProvider"/>).
    /// </summary>
    public TSelf ReplaceDbContext<TContext>() where TContext : DbContext
    {
        var services = Root.Services;
        RemoveExisting<TContext>(services);
        services.AddDbContext<TContext>(opt => UseProvider(opt, Source.ConnectionString));
        return (TSelf)this;
    }

    /// <summary>
    /// Replaces any registration of <typeparamref name="TContext"/>, then lets the caller
    /// further tweak the <see cref="DbContextOptionsBuilder"/> after provider binding.
    /// </summary>
    public TSelf ReplaceDbContext<TContext>(Action<DbContextOptionsBuilder> additionalConfiguration)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(additionalConfiguration);

        var services = Root.Services;
        RemoveExisting<TContext>(services);
        services.AddDbContext<TContext>(opt =>
        {
            UseProvider(opt, Source.ConnectionString);
            additionalConfiguration(opt);
        });
        return (TSelf)this;
    }

    /// <summary>Concrete providers bind the EF Core provider (SqlServer/Sqlite/Postgres/...).</summary>
    protected abstract void UseProvider(DbContextOptionsBuilder options, string connectionString);

    private static void RemoveExisting<TContext>(IServiceCollection services) where TContext : DbContext
    {
        var contextDescriptors = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                     || d.ServiceType == typeof(TContext))
            .ToArray();
        foreach (var d in contextDescriptors)
        {
            services.Remove(d);
        }
    }
}
