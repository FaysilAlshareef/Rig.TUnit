using NATS.Client.JetStream;
using NSubstitute;
using Rig.TUnit.Messaging.Nats.Topology;

namespace Rig.TUnit.Messaging.Nats.Tests.Unit.Topology;

// T054-RED: compile-fail until T054-GREEN adds INatsTopologyBuilder + NatsTopologyBuilder.
public sealed class NatsTopologyBuilderTests
{
    [Test]
    public async Task Ctor_NullJetStream_ThrowsArgumentNullException()
    {
        await Assert.That(() => new NatsTopologyBuilder(null!))  // CS0246 RED
            .Throws<ArgumentNullException>();
    }

    [Test]
    public async Task Stream_RecordsStreamDeclaration(CancellationToken ct)
    {
        // Arrange
        var mockJs  = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);  // CS0246 RED

        // Act
        builder.Stream("orders", cfg => cfg.WithSubjects("orders.>"));  // CS0246 RED

        // Assert — ApplyAsync should call CreateStreamAsync
        await builder.ApplyAsync(ct);  // CS0246 RED
        await mockJs.Received(1).CreateStreamAsync(
            Arg.Is<NATS.Client.JetStream.Models.StreamConfig>(c => c.Name == "orders"),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Stream_WithMaxMsgs_RecordsRetentionConfig(CancellationToken ct)
    {
        // Arrange
        var mockJs  = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);  // CS0246 RED

        // Act
        builder.Stream("capped", cfg => cfg.WithSubjects("capped.>").WithMaxMessages(100));  // CS0246 RED

        // Assert
        await builder.ApplyAsync(ct);  // CS0246 RED
        await mockJs.Received(1).CreateStreamAsync(
            Arg.Is<NATS.Client.JetStream.Models.StreamConfig>(c =>
                c.Name == "capped" && c.MaxMsgs == 100),
            Arg.Any<CancellationToken>());
    }
}
