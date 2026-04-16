using System.Diagnostics;
using Rig.TUnit.Core.Helpers;

namespace Rig.TUnit.Core.Tests.Unit.Helpers;

public class WaitHelperTests
{
    [Test]
    public async Task WaitForAsync_ConditionAlreadyTrue_CompletesWithoutPolling()
    {
        // Act & Assert — should complete without delay
        await WaitHelper.WaitForAsync(() => true, TimeSpan.FromSeconds(1));
    }

    [Test]
    public async Task WaitForAsync_ConditionBecomesTrueAfterPolls_CompletesSuccessfully()
    {
        // Arrange
        var callCount = 0;

        // Act
        await WaitHelper.WaitForAsync(() =>
        {
            callCount++;
            return callCount >= 3;
        }, TimeSpan.FromSeconds(5), pollingInterval: TimeSpan.FromMilliseconds(50));

        // Assert
        await Assert.That(callCount).IsGreaterThanOrEqualTo(3);
    }

    [Test]
    public async Task WaitForAsync_TimeoutExceeded_ThrowsTimeoutException()
    {
        // Act & Assert
        await Assert.That(async () =>
            await WaitHelper.WaitForAsync(() => false, TimeSpan.FromMilliseconds(200)))
            .ThrowsExactly<TimeoutException>();
    }

    [Test]
    public async Task WaitForAsync_TokenCancelled_ThrowsOperationCanceledException()
    {
        // Arrange
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        // Act & Assert — Task.Delay throws TaskCanceledException (subclass of OperationCanceledException)
        await Assert.That(async () =>
            await WaitHelper.WaitForAsync(() => false, TimeSpan.FromSeconds(10), cancellationToken: cts.Token))
            .ThrowsException()
            .WithMessageContaining("canceled");
    }

    [Test]
    public async Task WaitForAsync_AsyncConditionBecomesTrueAfterPolls_CompletesSuccessfully()
    {
        // Arrange
        var callCount = 0;

        // Act
        await WaitHelper.WaitForAsync(async () =>
        {
            await Task.Yield();
            callCount++;
            return callCount >= 2;
        }, TimeSpan.FromSeconds(5), pollingInterval: TimeSpan.FromMilliseconds(50));

        // Assert
        await Assert.That(callCount).IsGreaterThanOrEqualTo(2);
    }

    [Test]
    public async Task WaitForAsync_NoPollingIntervalSpecified_PollsWithDefaultInterval()
    {
        // Arrange — condition becomes true on second call, so must poll at least once
        var callCount = 0;
        var sw = Stopwatch.StartNew();

        // Act
        await WaitHelper.WaitForAsync(() =>
        {
            callCount++;
            return callCount >= 2;
        }, TimeSpan.FromSeconds(5));

        // Assert — with default ~250ms interval, at least one delay occurred
        sw.Stop();
        await Assert.That(sw.ElapsedMilliseconds).IsGreaterThanOrEqualTo(200);
    }

    [Test]
    public async Task WaitForResultAsync_ProducerReturnsNonNull_ReturnsProducedValue()
    {
        // Arrange
        var callCount = 0;

        // Act
        var result = await WaitHelper.WaitForResultAsync(async () =>
        {
            await Task.Yield();
            callCount++;
            return callCount >= 2 ? "found" : null;
        }, TimeSpan.FromSeconds(5), pollingInterval: TimeSpan.FromMilliseconds(50));

        // Assert
        await Assert.That(result).IsEqualTo("found");
    }

    [Test]
    public async Task WaitForResultAsync_ProducerNeverReturnsNonNull_ThrowsTimeoutException()
    {
        // Act & Assert
        await Assert.That(async () =>
            await WaitHelper.WaitForResultAsync<string>(
                async () => { await Task.Yield(); return null; },
                TimeSpan.FromMilliseconds(200)))
            .ThrowsExactly<TimeoutException>();
    }

    [Test]
    public async Task WaitForResultAsync_CustomPollingInterval_PollsAtExpectedRate()
    {
        // Arrange
        var callCount = 0;
        var sw = Stopwatch.StartNew();

        // Act — 100ms polling, condition met after 3 calls
        await WaitHelper.WaitForResultAsync(async () =>
        {
            await Task.Yield();
            callCount++;
            return callCount >= 3 ? "done" : null;
        }, TimeSpan.FromSeconds(5), pollingInterval: TimeSpan.FromMilliseconds(100));

        // Assert
        sw.Stop();
        await Assert.That(sw.ElapsedMilliseconds).IsGreaterThanOrEqualTo(150);
        await Assert.That(callCount).IsGreaterThanOrEqualTo(3);
    }
}
