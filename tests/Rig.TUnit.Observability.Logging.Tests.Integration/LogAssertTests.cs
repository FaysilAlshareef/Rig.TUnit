using Microsoft.Extensions.Logging;
using Rig.TUnit.Observability.Logging.Assertions;
using Rig.TUnit.Observability.Logging.Fixtures;

namespace Rig.TUnit.Observability.Logging.Tests.Integration;

public sealed class LogAssertTests
{
    private static async Task<LoggingFixture> CreateAsync()
    {
        var fx = new LoggingFixture();
        await fx.InitializeAsync();
        return fx;
    }

    [Test]
    public async Task Logged_Positive_FindsEntryAtLevel()
    {
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").LogInformation("Processing {OrderId}", 42);

        LogAssert.Logged(fx, LogLevel.Information)
                 .WithProperty("OrderId", 42)
                 .Once();
    }

    [Test]
    public async Task Logged_Negative_ThrowsWhenCountMismatches()
    {
        await using var fx = await CreateAsync();
        fx.CreateLogger("T").LogWarning("A");
        fx.CreateLogger("T").LogWarning("B");

        var threw = false;
        try { LogAssert.Logged(fx, LogLevel.Warning).Once(); }
        catch (LogAssertionException) { threw = true; }

        await Assert.That(threw).IsTrue();
    }

    [Test]
    public async Task Logged_InScope_FiltersByScopeKey()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        using (log.BeginScope(new Dictionary<string, object?> { ["UserId"] = "u-1" }))
        {
            log.LogInformation("in-scope");
        }
        log.LogInformation("out-of-scope");

        LogAssert.Logged(fx, LogLevel.Information)
                 .InScope("UserId", "u-1")
                 .Once();
    }

    [Test]
    public async Task Logged_Times_ExactCount()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        log.LogError("e1");
        log.LogError("e2");
        log.LogError("e3");

        LogAssert.Logged(fx, LogLevel.Error).Times(3);
    }

    [Test]
    public async Task Logged_WithMessage_FragmentFilter()
    {
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        log.LogInformation("Processing order {OrderId}", 1);
        log.LogInformation("Finished batch");

        LogAssert.Logged(fx, LogLevel.Information).WithMessage("Processing").Once();
    }

    [Test]
    public async Task Logged_Destructuring_AtPrefixAllowed()
    {
        // T226 — `{@Payload}` destructuring syntax must NOT be flagged.
        // It is a structured placeholder, not an interpolated literal.
        await using var fx = await CreateAsync();
        var log = fx.CreateLogger("T");
        var payload = new { Name = "x", Qty = 2 };
        log.LogInformation("Received {@Payload}", payload);

        await Assert.That(fx.Entries.Count).IsEqualTo(1);
        await Assert.That(fx.Entries[0].OriginalFormat).IsEqualTo("Received {@Payload}");
    }
}
