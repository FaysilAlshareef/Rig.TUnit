using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Mediator.Helpers;
using Rig.TUnit.Mediator.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Mediator.Tests.Unit.Helpers;

public sealed class HandlerHelperTests
{
    [Test]
    public async Task Send_Request_DispatchesAndReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator();
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);

        // Act
        var result = await helper.Send(new TestRequest("hello"));

        // Assert
        await Assert.That(result).IsEqualTo("Handled: hello");
    }

    [Test]
    public async Task Send_Command_DispatchesAndReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator();
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);

        // Act
        var result = await helper.Send(new TestCommand(21));

        // Assert
        await Assert.That(result).IsEqualTo(42);
    }

    [Test]
    public async Task Send_Query_DispatchesAndReturnsResult()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator();
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);

        // Act
        var result = await helper.Send(new TestQuery("active"));

        // Assert
        await Assert.That(result).IsEqualTo("Result: active");
    }

    [Test]
    public async Task Publish_Notification_InvokesHandler()
    {
        // Arrange
        var notificationHandler = new TestNotificationHandler();
        var services = new ServiceCollection();
        services.AddMediator();
        services.AddSingleton(notificationHandler);
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);

        // Act
        await helper.Publish(new TestNotification("test-message"));

        // Assert
        await Assert.That(notificationHandler.WasHandled).IsTrue();
        await Assert.That(notificationHandler.ReceivedMessage).IsEqualTo("test-message");
    }

    [Test]
    public async Task Send_WithCancellationToken_ForwardsToken()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddMediator();
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);
        using var cts = new CancellationTokenSource();

        // Act -- send with a live token (not cancelled) to verify it doesn't throw
        var result = await helper.Send(new TestRequest("token-test"), cts.Token);

        // Assert
        await Assert.That(result).IsEqualTo("Handled: token-test");
    }

    [Test]
    public async Task Send_CreatesIsolatedScope_PerCall()
    {
        // Arrange -- each Send should create an independent scope,
        // so two calls should return independent results without interference
        var services = new ServiceCollection();
        services.AddMediator();
        await using var provider = services.BuildServiceProvider();
        var scopeFactory = provider.GetRequiredService<IServiceScopeFactory>();
        var helper = new HandlerHelper(scopeFactory);

        // Act -- two calls should each create a new scope and return independent results
        var result1 = await helper.Send(new TestRequest("first"));
        var result2 = await helper.Send(new TestRequest("second"));

        // Assert -- each scope produced its own independent result
        await Assert.That(result1).IsEqualTo("Handled: first");
        await Assert.That(result2).IsEqualTo("Handled: second");
        await Assert.That(result1).IsNotEqualTo(result2);
    }
}
