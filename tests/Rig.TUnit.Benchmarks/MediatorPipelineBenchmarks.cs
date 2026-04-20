using BenchmarkDotNet.Attributes;
using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Rig.TUnit.Mediator.Helpers;

namespace Rig.TUnit.Benchmarks;

/// <summary>
/// Measures per-call overhead of
/// <see cref="HandlerHelper.Send{TResult}(IRequest{TResult}, CancellationToken)"/>
/// over a stubbed <see cref="IMediator"/>. Isolates the scope-creation + resolve
/// + dispatch cost from the handler body cost.
/// </summary>
[MemoryDiagnoser]
public class MediatorPipelineBenchmarks
{
    private ServiceProvider _provider = null!;
    private HandlerHelper _helper = null!;

    [GlobalSetup]
    public void Setup()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<Ping>(), Arg.Any<CancellationToken>())
            .Returns(callInfo => ValueTask.FromResult(((Ping)callInfo[0]).Value));

        var services = new ServiceCollection();
        services.AddSingleton(mediator);
        _provider = services.BuildServiceProvider();
        _helper = new HandlerHelper(_provider.GetRequiredService<IServiceScopeFactory>());
    }

    [Benchmark]
    public async ValueTask<int> Send_WithNewScope() => await _helper.Send(new Ping(42));

    [GlobalCleanup]
    public void Cleanup() => _provider.Dispose();

    public sealed record Ping(int Value) : IRequest<int>;
}
