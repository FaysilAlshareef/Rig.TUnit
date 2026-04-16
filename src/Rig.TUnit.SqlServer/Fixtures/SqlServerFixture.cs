using Rig.TUnit.Core.Builder;
using Testcontainers.MsSql;
using TUnit.Core.Interfaces;

namespace Rig.TUnit.SqlServer.Fixtures;

public sealed class SqlServerFixture : IAsyncInitializer, IAsyncDisposable, IRigConnectionSource
{
    public MsSqlContainer Container { get; } = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .Build();

    public string ConnectionString => Container.GetConnectionString();

    public async Task InitializeAsync() => await Container.StartAsync();
    public async ValueTask DisposeAsync() => await Container.DisposeAsync();
}
