using NSubstitute;
using Rig.TUnit.Messaging.Assertions;

namespace Rig.TUnit.Messaging.Tests.Unit;

public sealed class DeadLetterAssertTests
{
    [Test]
    public async Task HasMessage_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => DeadLetterAssert.HasMessage(null!, "reason"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task HasMessage_WhiteSpaceReason_ThrowsArgumentException()
    {
        var probe = Substitute.For<IDeadLetterProbe>();

        await Assert.That(() => DeadLetterAssert.HasMessage(probe, "  "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task HasMessage_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<IDeadLetterProbe>();
        probe.HasMessageAsync("reason", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DeadLetterAssert.HasMessage(probe, "reason");

        await probe.Received(1).HasMessageAsync("reason", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IsEmpty_NullProbe_ThrowsArgumentNullException()
    {
        await Assert.That(() => DeadLetterAssert.IsEmpty(null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IsEmpty_ValidArgs_DelegatesToProbe()
    {
        var probe = Substitute.For<IDeadLetterProbe>();
        probe.IsEmptyAsync(Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DeadLetterAssert.IsEmpty(probe);

        await probe.Received(1).IsEmptyAsync(Arg.Any<CancellationToken>());
    }
}
