using NSubstitute;
using Rig.TUnit.Databases.Assertions;
using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Tests.Unit;

public sealed class MigrationAssertTests
{
    [Test]
    public async Task AllApplied_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<IMigrationProbe>();

        await Assert.That(() => MigrationAssert.AllApplied(null!, probe))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AllApplied_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => MigrationAssert.AllApplied(rig, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AllApplied_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<IMigrationProbe>();
        probe.AllAppliedAsync(rig, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await MigrationAssert.AllApplied(rig, probe);

        await probe.Received(1).AllAppliedAsync(rig, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task NoPendingModelChanges_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<IMigrationProbe>();

        await Assert.That(() => MigrationAssert.NoPendingModelChanges(null!, probe))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task NoPendingModelChanges_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => MigrationAssert.NoPendingModelChanges(rig, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task NoPendingModelChanges_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<IMigrationProbe>();
        probe.NoPendingModelChangesAsync(rig, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await MigrationAssert.NoPendingModelChanges(rig, probe);

        await probe.Received(1).NoPendingModelChangesAsync(rig, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Idempotent_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<IMigrationProbe>();

        await Assert.That(() => MigrationAssert.Idempotent(null!, probe))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Idempotent_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => MigrationAssert.Idempotent(rig, null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Idempotent_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<IMigrationProbe>();
        probe.IdempotentAsync(rig, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await MigrationAssert.Idempotent(rig, probe);

        await probe.Received(1).IdempotentAsync(rig, Arg.Any<CancellationToken>());
    }
}
