namespace Rig.TUnit.Core.Extensions;

public static class EnvironmentDetection
{
    private static readonly string[] CiVariables =
    [
        "CI", "CONTINUOUS_INTEGRATION", "TF_BUILD", "GITHUB_ACTIONS",
        "JENKINS_URL", "GITLAB_CI", "TEAMCITY_VERSION", "CIRCLECI",
        "TRAVIS", "APPVEYOR", "CODEBUILD_BUILD_ID", "BUILD_BUILDID"
    ];

    /// <summary>Returns true if running in a CI/CD environment.</summary>
    public static bool IsRunningInCiCd() =>
        CiVariables.Any(v => !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(v)));
}
