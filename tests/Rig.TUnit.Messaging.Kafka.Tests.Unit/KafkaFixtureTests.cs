using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Kafka.Fixtures;
using Rig.TUnit.Messaging.Kafka.Options;

namespace Rig.TUnit.Messaging.Kafka.Tests.Unit;

/// <summary>
/// FR-035 unit tests for <see cref="KafkaFixture"/> — constructor variants, null-guards,
/// pre-initialize-state exceptions. No container — <c>InitializeAsync</c> is covered by the
/// integration suite.
/// </summary>
public sealed class KafkaFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new KafkaFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new KafkaFixtureOptions();
        await Assert.That(() => new KafkaFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new KafkaFixture((KafkaFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new KafkaFixtureOptions());
        await Assert.That(() => new KafkaFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new KafkaFixture((IOptions<KafkaFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new KafkaFixture();

        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TopicName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new KafkaFixture();

        var first = fx.TopicName;
        var second = fx.TopicName;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new KafkaFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
