using Rig.TUnit.Core.Extensions;

namespace Rig.TUnit.Core.Tests.Unit.Extensions;

[NotInParallel("EnvironmentVariables")]
public class EnvironmentDetectionTests
{
    private readonly Dictionary<string, string?> _originalValues = new();

    [Before(Test)]
    public void SaveAndClearCiVariables()
    {
        string[] ciVars = ["CI", "CONTINUOUS_INTEGRATION", "TF_BUILD", "GITHUB_ACTIONS",
            "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION", "CIRCLECI",
            "TRAVIS", "APPVEYOR", "CODEBUILD_BUILD_ID", "BUILD_BUILDID"];

        foreach (var v in ciVars)
        {
            _originalValues[v] = Environment.GetEnvironmentVariable(v);
            Environment.SetEnvironmentVariable(v, null);
        }
    }

    [After(Test)]
    public void RestoreCiVariables()
    {
        foreach (var (key, value) in _originalValues)
        {
            Environment.SetEnvironmentVariable(key, value);
        }
    }

    [Test]
    public async Task IsRunningInCiCd_GithubActionsSet_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("GITHUB_ACTIONS", "true");

        // Act
        var result = EnvironmentDetection.IsRunningInCiCd();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRunningInCiCd_CiVariableSet_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("CI", "true");

        // Act
        var result = EnvironmentDetection.IsRunningInCiCd();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRunningInCiCd_TfBuildSet_ReturnsTrue()
    {
        // Arrange
        Environment.SetEnvironmentVariable("TF_BUILD", "True");

        // Act
        var result = EnvironmentDetection.IsRunningInCiCd();

        // Assert
        await Assert.That(result).IsTrue();
    }

    [Test]
    public async Task IsRunningInCiCd_NoVariablesSet_ReturnsFalse()
    {
        // Act
        var result = EnvironmentDetection.IsRunningInCiCd();

        // Assert
        await Assert.That(result).IsFalse();
    }
}
