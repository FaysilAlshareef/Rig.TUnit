using Rig.TUnit.SqlServer.Fixtures;

namespace Rig.TUnit.SqlServer.Tests.Integration.Fixtures;

public class SqlServerFixtureTests
{
    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task InitializeAsync_StartsContainer(SqlServerFixture fixture)
    {
        // Assert — container should be running after initialization
        await Assert.That(fixture.Container).IsNotNull();
        await Assert.That(fixture.Container.State).IsEqualTo(DotNet.Testcontainers.Containers.TestcontainersStates.Running);
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task ConnectionString_AfterInit_IsValid(SqlServerFixture fixture)
    {
        // Assert — connection string should be non-empty and contain expected segments
        await Assert.That(fixture.ConnectionString).IsNotNull();
        await Assert.That(fixture.ConnectionString).Contains("Server=");
    }

    [Test]
    [ClassDataSource<SqlServerFixture>(Shared = SharedType.PerTestSession)]
    public async Task DisposeAsync_StopsContainer(SqlServerFixture _)
    {
        // Arrange — create a separate fixture to test disposal
        var fixture = new SqlServerFixture();
        await fixture.InitializeAsync();

        // Act
        await fixture.DisposeAsync();

        // Assert — container should no longer be running
        await Assert.That(fixture.Container.State).IsNotEqualTo(DotNet.Testcontainers.Containers.TestcontainersStates.Running);
    }
}
