using Rig.TUnit.Databases.NoSql.Helpers;

namespace Rig.TUnit.Databases.NoSql.Tests.Unit;

public sealed class ChangeFeedCaptureTests
{
    [Test]
    public async Task Captured_InitiallyEmpty()
    {
        var capture = new StubChangeFeedCapture();

        await Assert.That(capture.Captured.Count).IsEqualTo(0);
    }

    [Test]
    public async Task Record_NullDocument_ThrowsArgumentNullException()
    {
        var capture = new StubChangeFeedCapture();

        await Assert.That(() => capture.PublicRecord(null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Record_AddsDocumentToCaptured()
    {
        var capture = new StubChangeFeedCapture();

        capture.PublicRecord("doc-1");

        await Assert.That(capture.Captured.Count).IsEqualTo(1);
        await Assert.That(capture.Captured.First()).IsEqualTo("doc-1");
    }

    [Test]
    public async Task Captured_ReflectsMultipleRecords()
    {
        var capture = new StubChangeFeedCapture();

        capture.PublicRecord("a");
        capture.PublicRecord("b");
        capture.PublicRecord("c");

        await Assert.That(capture.Captured.Count).IsEqualTo(3);
    }

    [Test]
    public async Task StartAsync_ThenRecord_CapturedCountIsOne()
    {
        var capture = new StubChangeFeedCapture();
        await capture.StartAsync(CancellationToken.None);

        capture.PublicRecord("after-start");

        await Assert.That(capture.Captured.Count).IsEqualTo(1);
    }

    [Test]
    public async Task StopAsync_CapturedStatePreserved()
    {
        var capture = new StubChangeFeedCapture();
        capture.PublicRecord("doc");
        await capture.StopAsync(CancellationToken.None);

        await Assert.That(capture.Captured.Count).IsEqualTo(1);
    }

    private sealed class StubChangeFeedCapture : ChangeFeedCapture<string>
    {
        public void PublicRecord(string document) => Record(document);

        public override Task StartAsync(CancellationToken ct) => Task.CompletedTask;
        public override Task StopAsync(CancellationToken ct) => Task.CompletedTask;
    }
}
