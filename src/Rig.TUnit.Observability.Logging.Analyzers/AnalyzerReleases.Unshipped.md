; Unshipped analyzer release
; https://github.com/dotnet/roslyn-analyzers/blob/main/src/Microsoft.CodeAnalysis.Analyzers/ReleaseTrackingAnalyzers.Help.md

### New Rules

Rule ID | Category           | Severity | Notes
--------|--------------------|----------|------------------------------------------------------
RTU001  | RigTUnit.Logging   | Warning  | Interpolated string passed as log message template
RTU002  | RigTUnit.Logging   | Warning  | Console.Write used in a non-test source assembly
RTU003  | RigTUnit.Logging   | Warning  | PII-shaped property name in a log call
