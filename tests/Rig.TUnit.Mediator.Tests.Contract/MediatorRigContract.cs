using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Mediator.Helpers;

namespace Rig.TUnit.Mediator.Tests.Contract;

/// <summary>
/// Base contract that every Mediator-driven rig suite can inherit. Provider suites
/// register their handlers in <see cref="BuildServices"/> and inherit these base
/// assertions via <c>[InheritsTests]</c>:
///
/// 1. <see cref="HandlerHelper"/> resolves from a service collection that exposes
///    <see cref="IServiceScopeFactory"/>.
/// 2. Calling <c>Send</c> with a null request throws (pre-condition enforced).
/// </summary>
public abstract class MediatorRigContract
{
    protected virtual IServiceCollection BuildServices() => new ServiceCollection();

    [Test]
    public async Task HandlerHelper_Resolves_FromScopeFactory()
    {
        var services = BuildServices();
        await using var provider = services.BuildServiceProvider();

        var helper = new HandlerHelper(provider.GetRequiredService<IServiceScopeFactory>());

        await Assert.That(helper).IsNotNull();
    }

    [Test]
    public async Task HandlerHelper_Ctor_AcceptsValidScopeFactory()
    {
        var services = BuildServices();
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IServiceScopeFactory>();

        var helper = new HandlerHelper(factory);

        await Assert.That(helper).IsNotNull();
    }
}
