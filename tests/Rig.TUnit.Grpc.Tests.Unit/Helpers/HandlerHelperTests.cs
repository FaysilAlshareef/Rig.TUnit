using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.Grpc.Helpers;
using Rig.TUnit.Grpc.Tests.Unit.TestInfrastructure;

namespace Rig.TUnit.Grpc.Tests.Unit.Helpers;

public class HandlerHelperTests
{
    [Test]
    public async Task Send_WithTestRequest_DispatchesViaMediatR()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var scopeFactory = server.Services.GetRequiredService<IServiceScopeFactory>();
        var handler = new HandlerHelper(scopeFactory);

        // Act
        var result = await handler.Send(new TestRequest("hello"));

        // Assert
        await Assert.That(result).IsEqualTo("Handled: hello");
    }

    [Test]
    public async Task Send_CreatesNewScopePerCall_ReturnsIndependentResults()
    {
        // Arrange
        await using var server = new TestServerFactory();
        var scopeFactory = server.Services.GetRequiredService<IServiceScopeFactory>();
        var handler = new HandlerHelper(scopeFactory);

        // Act -- two calls should not interfere
        var result1 = await handler.Send(new TestRequest("first"));
        var result2 = await handler.Send(new TestRequest("second"));

        // Assert
        await Assert.That(result1).IsEqualTo("Handled: first");
        await Assert.That(result2).IsEqualTo("Handled: second");
    }
}
