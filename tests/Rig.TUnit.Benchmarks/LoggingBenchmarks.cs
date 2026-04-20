using BenchmarkDotNet.Attributes;
using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class LoggingBenchmarks
{
    [Benchmark]
    public LogEntry Construct_LogEntry()
        => new(DateTimeOffset.UtcNow, LogLevel.Information, "Cat", new EventId(1), "m", null,
            Array.Empty<KeyValuePair<string, object?>>(),
            Array.Empty<IReadOnlyList<KeyValuePair<string, object?>>>(), null);
}
