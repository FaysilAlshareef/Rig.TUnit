namespace Rig.TUnit.Observability.Logging.Options;

/// <summary>
/// Configuration for the runtime <c>AntiPatternDetector</c> and the companion Roslyn analyzer.
/// </summary>
public sealed class LoggingDetectorOptions
{
    public const string SectionName = "RigTUnit:Logging:Detector";

    /// <summary>When true, flags interpolated-template string literals (<c>$"..."</c>) passed to <c>ILogger.Log*</c>.</summary>
    public bool DetectInterpolatedTemplates { get; init; } = true;

    /// <summary>When true, the Roslyn analyzer flags <c>Console.Write*</c> / <c>Console.WriteLine*</c> in non-test assemblies.</summary>
    public bool DetectConsoleWrite { get; init; } = true;

    /// <summary>When true, flags log properties whose name matches the canonical PII list or any <see cref="AdditionalPiiPatterns"/>.</summary>
    public bool DetectPii { get; init; } = true;

    /// <summary>
    /// Extra PII-detection patterns. Treated as ECMAScript regex, case-insensitive,
    /// compiled once at detector startup. Matches any log property whose <i>name</i>
    /// matches the pattern.
    /// </summary>
    public IReadOnlyList<string> AdditionalPiiPatterns { get; init; } = Array.Empty<string>();

    /// <summary>
    /// Canonical, non-configurable PII token list. Tests verify these cannot be removed
    /// from the detector's effective list — they are always active when <see cref="DetectPii"/> is true.
    /// </summary>
    internal static readonly string[] CanonicalPiiTokens =
    [
        "password", "pwd", "secret", "token", "apikey", "api_key",
        "authorization", "auth", "credential", "credentials",
        "ssn", "socialsecurity", "social_security",
        "creditcard", "credit_card", "cardnumber", "card_number", "cvv", "cvc",
        "email", "phone", "phonenumber", "phone_number",
        "dob", "dateofbirth", "date_of_birth",
        "address", "streetaddress", "street_address",
        "connectionstring", "connection_string",
    ];
}
