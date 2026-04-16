using Rig.TUnit.Core.Fixtures;
using TUnit.Core.Interfaces;

namespace Rig.TUnit.Core.Tests.Unit.Fixtures;

public class CompositeFixtureTests
{
    [Test]
    public async Task InitializeAsync_AllFixtures_InitializesInParallel()
    {
        // Arrange
        var fixture1 = new TrackingFixture("A");
        var fixture2 = new TrackingFixture("B");
        var fixture3 = new TrackingFixture("C");
        var composite = new CompositeFixture(fixture1, fixture2, fixture3);

        // Act
        await composite.InitializeAsync();

        // Assert — all should be initialized
        await Assert.That(fixture1.Initialized).IsTrue();
        await Assert.That(fixture2.Initialized).IsTrue();
        await Assert.That(fixture3.Initialized).IsTrue();
    }

    [Test]
    public async Task DisposeAsync_AllFixtures_DisposesInReverseOrder()
    {
        // Arrange
        var disposeOrder = new List<string>();
        var fixture1 = new TrackingFixture("A", disposeOrder);
        var fixture2 = new TrackingFixture("B", disposeOrder);
        var fixture3 = new TrackingFixture("C", disposeOrder);
        var composite = new CompositeFixture(fixture1, fixture2, fixture3);

        // Act
        await composite.DisposeAsync();

        // Assert — LIFO: C, B, A
        await Assert.That(disposeOrder).IsEquivalentTo(["C", "B", "A"]);
    }

    [Test]
    public async Task Get_ExistingType_ReturnsFixture()
    {
        // Arrange
        var fixture = new TrackingFixture("test");
        var composite = new CompositeFixture(fixture);

        // Act
        var result = composite.Get<TrackingFixture>();

        // Assert
        await Assert.That(result).IsSameReferenceAs(fixture);
    }

    [Test]
    public async Task Get_MissingType_ThrowsInvalidOperationException()
    {
        // Arrange
        var composite = new CompositeFixture(new TrackingFixture("only"));

        // Act & Assert
        await Assert.That(() => composite.Get<OtherFixture>())
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task InitializeAsync_NonInitializableFixtures_Ignored()
    {
        // Arrange — plain object mixed with IAsyncInitializer
        var initFixture = new TrackingFixture("init");
        var plainObject = new object();
        var composite = new CompositeFixture(plainObject, initFixture);

        // Act — should not throw
        await composite.InitializeAsync();

        // Assert
        await Assert.That(initFixture.Initialized).IsTrue();
    }

    // --- Test helpers ---

    internal sealed class TrackingFixture(string name, List<string>? disposeOrder = null) : IAsyncInitializer, IAsyncDisposable
    {
        public bool Initialized { get; private set; }

        public Task InitializeAsync()
        {
            Initialized = true;
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            disposeOrder?.Add(name);
            return ValueTask.CompletedTask;
        }
    }

    private sealed class OtherFixture;
}
