using Rig.TUnit.Parallelism.Helpers;

namespace Rig.TUnit.Parallelism.Tests.Unit;

/// <summary>
/// Pure unit coverage for <see cref="PortAllocator"/> + <see cref="ExclusiveResourceCoordinator"/>.
/// Concurrent-behaviour tests live in <c>Rig.TUnit.Parallelism.Tests.Integration</c> where
/// async coordination assertions are the point.
/// </summary>
public sealed class ParallelismUnitTests
{
    [Test]
    public async Task Allocate_WithNoArgs_ReturnsPortInValidRange()
    {
        var port = PortAllocator.Allocate();

        await Assert.That(port).IsGreaterThan(0);
        await Assert.That(port).IsLessThanOrEqualTo(65535);
    }

    [Test]
    public async Task Allocate_WithCountFive_ReturnsFiveDistinctPorts()
    {
        var ports = PortAllocator.Allocate(5);

        await Assert.That(ports.Count).IsEqualTo(5);
        await Assert.That(ports.Distinct().Count()).IsEqualTo(5);
    }

    [Test]
    public async Task Allocate_WithZeroCount_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => PortAllocator.Allocate(0)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Allocate_WithNegativeCount_ThrowsArgumentOutOfRange()
    {
        await Assert.That(() => PortAllocator.Allocate(-1)).ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task AcquireAsync_WithValidResource_ReturnsDisposable()
    {
        var resource = $"test-{Guid.NewGuid():N}";

        using var handle = await ExclusiveResourceCoordinator.AcquireAsync(resource);

        await Assert.That(handle).IsNotNull();
    }

    [Test]
    public async Task AcquireAsync_WithEmptyResource_ThrowsArgumentException()
    {
        await Assert.That(async () => await ExclusiveResourceCoordinator.AcquireAsync(""))
            .Throws<ArgumentException>();
    }

    [Test]
    public async Task AcquireAsync_WithNullResource_ThrowsArgumentException()
    {
        await Assert.That(async () => await ExclusiveResourceCoordinator.AcquireAsync(null!))
            .Throws<ArgumentException>();
    }
}
