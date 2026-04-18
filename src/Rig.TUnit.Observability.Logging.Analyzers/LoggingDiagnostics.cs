using Microsoft.CodeAnalysis;

namespace Rig.TUnit.Observability.Logging.Analyzers;

internal static class LoggingDiagnostics
{
    public const string Category = "RigTUnit.Logging";

    public static readonly DiagnosticDescriptor InterpolatedTemplate = new(
        id: "RTU001",
        title: "Interpolated string passed as log message template",
        messageFormat: "Do not pass an interpolated string (\"$...\") to ILogger; use a structured template with {{Placeholders}}",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Interpolated strings bake values into the message at compile time, losing structured data for downstream sinks.");

    public static readonly DiagnosticDescriptor ConsoleWrite = new(
        id: "RTU002",
        title: "Console.Write used in a non-test source assembly",
        messageFormat: "Do not use Console.{0} in production/source code; use ILogger<T> instead",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Console.Write bypasses the logging pipeline and cannot be captured, filtered, or correlated.");

    public static readonly DiagnosticDescriptor PiiPropertyName = new(
        id: "RTU003",
        title: "PII-shaped property name in a log call",
        messageFormat: "Log property '{0}' matches a PII-shaped token — do not log sensitive user data",
        category: Category,
        defaultSeverity: DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "Property names matching canonical PII tokens (password, token, ssn, email, ...) should never appear in logs.");
}
