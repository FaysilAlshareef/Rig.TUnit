using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Core.Tests.Unit.Builder;

public class ConnectionSourceTests
{
    // --- ConfigConnectionSource ---

    [Test]
    public async Task ConfigConnectionSource_ValidKey_ReturnsValue()
    {
        // Arrange
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Db"] = "Server=test" })
            .Build();
        var source = RigConnect.FromConfig(config, "ConnectionStrings:Db");

        // Act
        var result = source.ConnectionString;

        // Assert
        await Assert.That(result).IsEqualTo("Server=test");
    }

    [Test]
    public async Task ConfigConnectionSource_MissingKey_ThrowsInvalidOperationException()
    {
        // Arrange
        var config = new ConfigurationBuilder().Build();
        var source = RigConnect.FromConfig(config, "Missing:Key");

        // Act & Assert
        await Assert.That(() => _ = source.ConnectionString)
            .ThrowsExactly<InvalidOperationException>();
    }

    // --- OptionsConnectionSource ---

    [Test]
    public async Task OptionsConnectionSource_ValidSelector_ReturnsValue()
    {
        // Arrange
        var options = Options.Create(new TestOptions { ConnectionString = "Server=options" });
        var source = RigConnect.FromOptions(options, o => o.ConnectionString);

        // Act
        var result = source.ConnectionString;

        // Assert
        await Assert.That(result).IsEqualTo("Server=options");
    }

    [Test]
    public async Task OptionsConnectionSource_NullSelector_ThrowsInvalidOperationException()
    {
        // Arrange
        var options = Options.Create(new TestOptions { ConnectionString = null! });
        var source = RigConnect.FromOptions(options, o => o.ConnectionString);

        // Act & Assert
        await Assert.That(() => _ = source.ConnectionString)
            .ThrowsExactly<InvalidOperationException>();
    }

    // --- ValueConnectionSource ---

    [Test]
    public async Task ValueConnectionSource_ValidString_ReturnsValue()
    {
        // Arrange
        var source = RigConnect.FromValue("Server=value");

        // Act
        var result = source.ConnectionString;

        // Assert
        await Assert.That(result).IsEqualTo("Server=value");
    }

    [Test]
    public async Task ValueConnectionSource_NullString_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.That(() => RigConnect.FromValue(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    // --- AutoConnectionSource ---
    // Each test independently manages its own env state via try/finally.
    // [NotInParallel] prevents concurrent env var mutations.

    private static readonly string[] CiEnvVars =
        ["CI", "CONTINUOUS_INTEGRATION", "TF_BUILD", "GITHUB_ACTIONS", "JENKINS_URL",
         "GITLAB_CI", "TEAMCITY_VERSION", "CIRCLECI", "TRAVIS", "APPVEYOR",
         "CODEBUILD_BUILD_ID", "BUILD_BUILDID"];

    private static void ClearAllCiEnvVars()
    {
        foreach (var v in CiEnvVars)
            Environment.SetEnvironmentVariable(v, null);
    }

    [Test]
    [NotInParallel("EnvVarTests")]
    public async Task AutoConnectionSource_InCi_ReturnsFixtureConnectionString()
    {
        ClearAllCiEnvVars();
        Environment.SetEnvironmentVariable("CI", "true");
        try
        {
            // Arrange
            var fixture = new FakeFixture("Server=container");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Db"] = "Server=external" })
                .Build();
            var source = RigConnect.Auto(fixture, config, "ConnectionStrings:Db");

            // Act
            var result = source.ConnectionString;

            // Assert
            await Assert.That(result).IsEqualTo("Server=container");
        }
        finally
        {
            ClearAllCiEnvVars();
        }
    }

    [Test]
    [NotInParallel("EnvVarTests")]
    public async Task AutoConnectionSource_LocalWithConfig_ReturnsConfigValue()
    {
        ClearAllCiEnvVars();
        try
        {
            // Arrange
            var fixture = new FakeFixture("Server=container");
            var config = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?> { ["ConnectionStrings:Db"] = "Server=external" })
                .Build();
            var source = RigConnect.Auto(fixture, config, "ConnectionStrings:Db");

            // Act
            var result = source.ConnectionString;

            // Assert
            await Assert.That(result).IsEqualTo("Server=external");
        }
        finally
        {
            ClearAllCiEnvVars();
        }
    }

    [Test]
    [NotInParallel("EnvVarTests")]
    public async Task AutoConnectionSource_LocalWithoutConfig_FallsBackToFixture()
    {
        ClearAllCiEnvVars();
        try
        {
            // Arrange
            var fixture = new FakeFixture("Server=container");
            var config = new ConfigurationBuilder().Build();
            var source = RigConnect.Auto(fixture, config, "ConnectionStrings:Missing");

            // Act
            var result = source.ConnectionString;

            // Assert
            await Assert.That(result).IsEqualTo("Server=container");
        }
        finally
        {
            ClearAllCiEnvVars();
        }
    }

    // --- Test helpers ---

    private sealed class FakeFixture(string connectionString) : IRigConnectionSource
    {
        public string ConnectionString { get; } = connectionString;
    }

    private sealed class TestOptions
    {
        public string ConnectionString { get; set; } = null!;
    }
}
