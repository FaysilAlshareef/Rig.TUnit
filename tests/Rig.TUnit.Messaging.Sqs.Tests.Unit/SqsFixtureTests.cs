using Microsoft.Extensions.Options;
using Rig.TUnit.Messaging.Sqs.Fixtures;
using Rig.TUnit.Messaging.Sqs.Options;

namespace Rig.TUnit.Messaging.Sqs.Tests.Unit;

public sealed class SqsFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new SqsFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new SqsFixtureOptions();
        await Assert.That(() => new SqsFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new SqsFixture((SqsFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new SqsFixtureOptions());
        await Assert.That(() => new SqsFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new SqsFixture((IOptions<SqsFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new SqsFixture();

        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new SqsFixture();

        await Assert.That(() => { _ = fx.Client; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task TopicName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new SqsFixture();

        var first = fx.TopicName;
        var second = fx.TopicName;

        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new SqsFixture();

        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
