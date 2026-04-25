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

    [Test]
    public async Task Stream_WithRetentionPolicy_Limits_MapsToStreamConfigRetentionLimits(CancellationToken ct)
    {
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);

        builder.Stream("limits-stream", cfg => cfg
            .WithSubjects("x.>")
            .WithRetentionPolicy(NatsRetentionPolicy.Limits));

        await builder.ApplyAsync(ct);
        await mockJs.Received(1).CreateStreamAsync(
            Arg.Is<NATS.Client.JetStream.Models.StreamConfig>(c =>
                c.Name == "limits-stream"
                && c.Retention == NATS.Client.JetStream.Models.StreamConfigRetention.Limits),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Stream_WithRetentionPolicy_Interest_MapsToStreamConfigRetentionInterest(CancellationToken ct)
    {
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);

        builder.Stream("interest-stream", cfg => cfg
            .WithSubjects("y.>")
            .WithRetentionPolicy(NatsRetentionPolicy.Interest));

        await builder.ApplyAsync(ct);
        await mockJs.Received(1).CreateStreamAsync(
            Arg.Is<NATS.Client.JetStream.Models.StreamConfig>(c =>
                c.Name == "interest-stream"
                && c.Retention == NATS.Client.JetStream.Models.StreamConfigRetention.Interest),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Stream_WithRetentionPolicy_WorkQueue_MapsToStreamConfigRetentionWorkqueue(CancellationToken ct)
    {
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);

        builder.Stream("work-stream", cfg => cfg
            .WithSubjects("z.>")
            .WithRetentionPolicy(NatsRetentionPolicy.WorkQueue));

        await builder.ApplyAsync(ct);
        await mockJs.Received(1).CreateStreamAsync(
            Arg.Is<NATS.Client.JetStream.Models.StreamConfig>(c =>
                c.Name == "work-stream"
                && c.Retention == NATS.Client.JetStream.Models.StreamConfigRetention.Workqueue),
            Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Stream_NullName_ThrowsArgumentException(CancellationToken ct)
    {
        var mockJs = Substitute.For<INatsJSContext>();
        var builder = new NatsTopologyBuilder(mockJs);

        await Assert.That(() => builder.Stream(null!, cfg => cfg.WithSubjects("a.>")))
            .Throws<ArgumentException>();
        // No CreateStreamAsync should be queued
        await builder.ApplyAsync(ct);
        await mockJs.DidNotReceive().CreateStreamAsync(
            Arg.Any<NATS.Client.JetStream.Models.StreamConfig>(),
            Arg.Any<CancellationToken>());
    }
}
