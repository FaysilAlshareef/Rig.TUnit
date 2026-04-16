using Rig.TUnit.ServiceBus.Fixtures;
using Rig.TUnit.ServiceBus.Helpers;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Helpers;

[NotInParallel("ServiceBus")]
public class ListenerHelperTests
{
    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task WaitForMessages_CapturesMessage(ServiceBusFixture fixture)
    {
        // Arrange
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");

        // Act
        await sender.SendAsync(new { Name = "TestEvent" });
        await listener.WaitForMessagesAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        await Assert.That(listener.Messages.Count).IsEqualTo(1);
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task WaitForMessages_TimeoutExceeded_ThrowsTimeoutException(ServiceBusFixture fixture)
    {
        // Arrange
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();

        // Act & Assert
        await Assert.That(async () =>
            await listener.WaitForMessagesAsync(expectedCount: 100, timeout: TimeSpan.FromSeconds(2)))
            .ThrowsExactly<TimeoutException>();
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task WaitForMessages_ExpectedCountReached_Returns(ServiceBusFixture fixture)
    {
        // Arrange
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");

        // Act
        await sender.SendAsync(new { Id = 1 });
        await sender.SendAsync(new { Id = 2 });
        await listener.WaitForMessagesAsync(expectedCount: 2, timeout: TimeSpan.FromSeconds(30));

        // Assert
        await Assert.That(listener.Messages.Count).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task StartAsync_ThenDispose_Lifecycle(ServiceBusFixture fixture)
    {
        // Arrange
        var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");

        // Act
        await listener.StartAsync();
        await listener.StopAsync();
        await listener.DisposeAsync();

        // Assert — verify the listener has no collected errors after lifecycle
        await Assert.That(listener.Errors.Count).IsEqualTo(0);
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task Messages_AfterCapture_ContainsReceivedMessage(ServiceBusFixture fixture)
    {
        // Arrange
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");

        // Act
        await sender.SendAsync(new { Type = "Capture" });
        await listener.WaitForMessagesAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        var message = listener.Messages.First();
        await Assert.That(message.ContentType).IsEqualTo("application/json");
    }
}
