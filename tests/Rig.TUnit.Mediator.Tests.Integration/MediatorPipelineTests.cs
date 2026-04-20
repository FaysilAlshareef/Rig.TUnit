using Mediator;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Rig.TUnit.Mediator.Helpers;

namespace Rig.TUnit.Mediator.Tests.Integration;

/// <summary>
/// End-to-end exercises of <see cref="HandlerHelper"/> against a real
/// <see cref="ServiceProvider"/>. Each test uses a <see cref="Substitute"/> IMediator
/// so we exercise the helper's scope lifecycle without pulling the Mediator source
/// generator into this test assembly.
/// </summary>
public sealed class MediatorPipelineTests
{
    [Test]
    public async Task HandlerHelper_Send_Request_ResolvesMediatorFromScope()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<PingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(7));

        await using var provider = BuildProvider(mediator);
        var helper = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await helper.Send(new PingRequest(3));

        await Assert.That(result).IsEqualTo(7);
        await mediator.Received(1).Send(Arg.Any<PingRequest>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandlerHelper_Send_Command_DispatchesThroughNewScope()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<DoWork>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult("done"));

        await using var provider = BuildProvider(mediator);
        var helper = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await helper.Send(new DoWork());

        await Assert.That(result).IsEqualTo("done");
    }

    [Test]
    public async Task HandlerHelper_Send_Query_ReturnsValueFromHandler()
    {
        var mediator = Substitute.For<IMediator>();
        mediator.Send(Arg.Any<GetThing>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(42));

        await using var provider = BuildProvider(mediator);
        var helper = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        var result = await helper.Send(new GetThing());

        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task HandlerHelper_Publish_FiresOnMediator()
    {
        var mediator = Substitute.For<IMediator>();

        await using var provider = BuildProvider(mediator);
        var helper = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        await helper.Publish(new Notified(42));

        await mediator.Received(1).Publish(Arg.Any<Notified>(), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task HandlerHelper_CreatesFreshScope_PerCall()
    {
        var factory = Substitute.For<IServiceScopeFactory>();
        var scope = Substitute.For<IServiceScope>();
        var mediator = Substitute.For<IMediator>();

        var provider = Substitute.For<IServiceProvider>();
        provider.GetService(typeof(IMediator)).Returns(mediator);
        scope.ServiceProvider.Returns(provider);
        factory.CreateScope().Returns(scope);

        var helper = new HandlerHelper(factory);
        mediator.Send(Arg.Any<PingRequest>(), Arg.Any<CancellationToken>())
            .Returns(ValueTask.FromResult(1));

        await helper.Send(new PingRequest(1));
        await helper.Send(new PingRequest(2));

        factory.Received(2).CreateScope();
    }

    private static ServiceProvider BuildProvider(IMediator mediator)
    {
        var services = new ServiceCollection();
        services.AddSingleton(mediator);
        return services.BuildServiceProvider();
    }

    public sealed record PingRequest(int Value) : IRequest<int>;

    public sealed record DoWork : ICommand<string>;

    public sealed record GetThing : IQuery<int>;

    public sealed record Notified(int Value) : INotification;
}
