using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.Extensions.Logging;

namespace Rig.TUnit.Observability.Logging.Analyzers.Tests.Unit;

/// <summary>
/// Minimal in-process harness for running <see cref="DiagnosticAnalyzer"/> over a
/// source snippet and returning the emitted diagnostics. Avoids a full Roslyn
/// testing-framework dependency since we only need single-file assertions.
/// </summary>
internal static class AnalyzerHarness
{
    private static readonly MetadataReference[] DefaultReferences =
    {
        MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(ILogger).Assembly.Location),
        MetadataReference.CreateFromFile(typeof(LoggerExtensions).Assembly.Location),
        MetadataReference.CreateFromFile(System.IO.Path.Combine(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Runtime.dll")),
        MetadataReference.CreateFromFile(System.IO.Path.Combine(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "netstandard.dll")),
        MetadataReference.CreateFromFile(System.IO.Path.Combine(
            System.Runtime.InteropServices.RuntimeEnvironment.GetRuntimeDirectory(), "System.Private.CoreLib.dll")),
    };

    public static async Task<ImmutableArray<Diagnostic>> RunAsync(
        string source,
        string assemblyName = "TestAssembly")
    {
        var tree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Latest));
        var compilation = CSharpCompilation.Create(
            assemblyName,
            new[] { tree },
            DefaultReferences,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable));

        var analyzer = new LoggingAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diags = await withAnalyzers.GetAnalyzerDiagnosticsAsync();
        return diags;
    }
}
