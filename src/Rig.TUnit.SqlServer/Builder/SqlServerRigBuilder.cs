using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Core.Builder;
using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.SqlServer.Builder;

/// <summary>
/// Fluent builder that wires SQL Server test infrastructure into an <see cref="IServiceCollection"/>.
/// Each <see cref="ReplaceDbContext{TContext}()"/> call produces a database isolated by a GUID suffix.
/// </summary>
/// <remarks>
/// This is test infrastructure. The per-call database materialization is intentional — each builder
/// invocation produces an isolated database so parallel tests do not interfere with one another.
/// </remarks>
public sealed class SqlServerRigBuilder
{
    private readonly IServiceCollection _services;
    private readonly IRigConnectionSource _source;

    internal SqlServerRigBuilder(IServiceCollection services, IRigConnectionSource source)
    {
        _services = services;
        _source = source;
    }

    /// <summary>
    /// Replaces the <typeparamref name="TContext"/> registration with a new registration that targets
    /// an isolated database (<c>test_{guid}</c>) built on top of the source connection string.
    /// The DbContext is registered as Scoped.
    /// </summary>
    public SqlServerRigBuilder ReplaceDbContext<TContext>() where TContext : DbContext
        => ReplaceDbContext<TContext>(baseConnectionString =>
            $"{baseConnectionString};Database=test_{Guid.NewGuid():N}");

    /// <summary>
    /// Replaces the <typeparamref name="TContext"/> registration with a caller-supplied connection string
    /// transform. The DbContext is registered as Scoped. Use this overload when you need to fully control
    /// the target database name or options.
    /// </summary>
    public SqlServerRigBuilder ReplaceDbContext<TContext>(
        Func<string, string> connectionStringFactory) where TContext : DbContext
    {
        ArgumentNullException.ThrowIfNull(connectionStringFactory);

        _services.RemoveByName(typeof(TContext).Name);

        var connectionString = connectionStringFactory(_source.ConnectionString);

        // Materialize the isolated database up-front without going through DI so test runs
        // do not build a throwaway ServiceProvider.
        var options = new DbContextOptionsBuilder<TContext>()
            .UseSqlServer(connectionString)
            .Options;

        using (var ctx = (TContext)Activator.CreateInstance(typeof(TContext), options)!)
        {
            ctx.Database.EnsureCreated();
        }

        // AddDbContext registers the DbContext with the default Scoped lifetime.
        _services.AddDbContext<TContext>(o => o.UseSqlServer(connectionString));

        return this;
    }
}
