using Newtonsoft.Json;
using Rig.TUnit.ServiceBus.Fixtures;
using Rig.TUnit.ServiceBus.Helpers;

namespace Rig.TUnit.ServiceBus.Tests.Integration.Helpers;

[NotInParallel("ServiceBus")]
public class ServiceBusEventSenderTests
{
    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task SendAsync_PublishesReceivableMessage(ServiceBusFixture fixture)
    {
        // Arrange
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();

        // Act
        await sender.SendAsync(new { Name = "Receivable" });
        await listener.WaitForMessagesAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        await Assert.That(listener.Messages.Count).IsGreaterThanOrEqualTo(1);
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task SendAsync_SetsSessionId(ServiceBusFixture fixture)
    {
        // Arrange
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();

        // Act
        await sender.SendAsync(new { Data = "session-test" }, sessionId: "my-session");
        await listener.WaitForMessagesAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        var msg = listener.Messages.First();
        await Assert.That(msg.SessionId).IsEqualTo("my-session");
    }

    [Test]
    [ClassDataSource<ServiceBusFixture>(Shared = SharedType.PerTestSession)]
    public async Task SendAsync_SerializesAsJson(ServiceBusFixture fixture)
    {
        // Arrange
        await using var sender = new ServiceBusEventSender(fixture.ConnectionString, "test-topic");
        await using var listener = new ListenerHelper(fixture.ConnectionString, "test-topic", "test-subscription");
        await listener.StartAsync();

        var data = new { Key = "value", Number = 42 };

        // Act
        await sender.SendAsync(data);
        await listener.WaitForMessagesAsync(expectedCount: 1, timeout: TimeSpan.FromSeconds(30));

        // Assert
        var msg = listener.Messages.First();
        var body = msg.Body.ToString();
        var deserialized = JsonConvert.DeserializeAnonymousType(body, new { Key = "", Number = 0 });
        await Assert.That(deserialized!.Key).IsEqualTo("value");
        await Assert.That(deserialized.Number).IsEqualTo(42);
    }
}
