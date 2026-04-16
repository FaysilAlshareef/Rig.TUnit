using BenchmarkDotNet.Attributes;
using Grpc.Net.Client;

namespace Rig.TUnit.Benchmarks;

[MemoryDiagnoser]
public class GrpcBenchmarks
{
    [Benchmark]
    public GrpcChannel ChannelCreation()
    {
        var channel = GrpcChannel.ForAddress("http://localhost:5000");
        return channel;
    }
}
