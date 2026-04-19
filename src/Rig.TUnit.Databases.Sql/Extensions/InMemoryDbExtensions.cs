using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Sql.Extensions;

/// <summary>
/// Extension points for binding an EF Core <c>DbContext</c> to the in-memory provider.
/// Fastest fast-path — no container, no persistence; ideal for handler-level tests.
/// </summary>
public static class InMemoryDbExtensions
{
    /// <summary>
    /// Replaces any registration of <typeparamref name="TContext"/> with an in-memory
    /// provider bound to the rig's isolation key.
    /// </summary>
    public static RigBuilder UseInMemoryDb<TContext>(this RigBuilder rig, string databaseName)
        where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentException.ThrowIfNullOrWhiteSpace(databaseName);

        var services = rig.Services;
        var existing = services
            .Where(d => d.ServiceType == typeof(DbContextOptions<TContext>)
                     || d.ServiceType == typeof(TContext))
            .ToArray();
        foreach (var d in existing)
        {
            services.Remove(d);
        }

        services.AddDbContext<TContext>(opt =>
        {
            opt.UseInMemoryDatabase(databaseName);
            opt.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
        });
        return rig;
    }
}
