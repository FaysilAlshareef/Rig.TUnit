using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Rig.TUnit.Grpc.Helpers;

/// <summary>
/// Test infrastructure helper that dispatches MediatR requests within isolated scopes.
/// Uses IServiceScopeFactory to create a new scope per Send call, simulating per-request lifetime.
/// </summary>
public sealed class HandlerHelper(IServiceScopeFactory scopeFactory)
{
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;

    /// <summary>
    /// Dispatches a MediatR request within a new service scope.
    /// </summary>
    public async Task<TResult> Send<TResult>(IRequest<TResult> request)
    {
        using var scope = _scopeFactory.CreateScope();
        var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
        return await mediator.Send(request);
    }
}
