using Rig.TUnit.Databases.NoSql.Cosmos.Fixtures;

namespace Rig.TUnit.Databases.NoSql.Cosmos.Tests.Integration;

/// <summary>
/// Live Cosmos emulator tests. Skipped on Windows runners — the Linux
/// emulator image only runs under Linux containers (testcontainers-dotnet#1306).
/// </summary>
[Category("cosmos")]
public sealed class CosmosContainerLiveTests
{
    [Test]
    public async Task Initialize_StartsContainer_ExposesConnectionString()
    {
        if (OperatingSystem.IsWindows()) return;

        await using var fx = new CosmosFixture();
        await fx.InitializeAsync();

        var conn = fx.ConnectionString;
        await Assert.That(conn).Contains("AccountEndpoint");
        await Assert.That(conn).Contains("AccountKey");
    }
}
