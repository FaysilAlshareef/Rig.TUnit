using Mediator;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Mediator.Helpers;

/// <summary>
/// Test infrastructure helper that dispatches Mediator requests within isolated scopes.
/// Uses IServiceScopeFactory to create a new scope per call, simulating per-request lifetime.
/// </summary>
public sealed class HandlerHelper(IServiceScopeFactory scopeFactory)
{
    /// <summary>
    /// Dispatches a Mediator request within a new service scope.
    /// </summary>
    public async ValueTask<TResult> Send<TResult>(IRequest<TResult> request, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request, cancellationToken);
    }

    /// <summary>
    /// Dispatches a Mediator command within a new service scope.
    /// </summary>
    public async ValueTask<TResult> Send<TResult>(ICommand<TResult> command, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(command, cancellationToken);
    }

    /// <summary>
    /// Dispatches a Mediator query within a new service scope.
    /// </summary>
    public async ValueTask<TResult> Send<TResult>(IQuery<TResult> query, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(query, cancellationToken);
    }

    /// <summary>
    /// Publishes a Mediator notification within a new service scope.
    /// </summary>
    public async ValueTask Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
        where TNotification : INotification
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        await mediator.Publish(notification, cancellationToken);
    }
}
