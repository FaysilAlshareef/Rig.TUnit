using Rig.TUnit.Observability.Metrics.Helpers;

namespace Rig.TUnit.Observability.Metrics.Tests.Unit;

public sealed class TagCardinalityGuardTests
{
    [Test]
    public async Task EnsureWithinBudget_UnderBudget_ReturnsTrue()
    {
        var ok = TagCardinalityGuard.EnsureWithinBudget("tenant", distinctCount: 5, maxCardinality: 100);
        await Assert.That(ok).IsTrue();
    }

    [Test]
    public async Task EnsureWithinBudget_ExactlyAtBudget_ReturnsTrue()
    {
        var ok = TagCardinalityGuard.EnsureWithinBudget("tenant", distinctCount: 100, maxCardinality: 100);
        await Assert.That(ok).IsTrue();
    }

    [Test]
    public async Task EnsureWithinBudget_ExceedsBudget_Throws()
    {
        await Assert.That(() => TagCardinalityGuard.EnsureWithinBudget("tenant", distinctCount: 101, maxCardinality: 100))
            .ThrowsExactly<CardinalityException>();
    }

    [Test]
    public async Task EnsureWithinBudget_NullTagName_Throws()
    {
        await Assert.That(() => TagCardinalityGuard.EnsureWithinBudget(null!, 1, 10))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task EnsureWithinBudget_NegativeDistinctCount_Throws()
    {
        await Assert.That(() => TagCardinalityGuard.EnsureWithinBudget("tag", -1, 10))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task EnsureWithinBudget_ZeroMaxCardinality_Throws()
    {
        await Assert.That(() => TagCardinalityGuard.EnsureWithinBudget("tag", 1, 0))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task CardinalityException_Message_IncludesTagAndCount()
    {
        var threw = false;
        try
        {
            TagCardinalityGuard.EnsureWithinBudget("tenant", distinctCount: 250, maxCardinality: 100);
        }
        catch (CardinalityException ex)
        {
            threw = true;
            await Assert.That(ex.Message).Contains("tenant");
            await Assert.That(ex.Message).Contains("250");
            await Assert.That(ex.Message).Contains("100");
        }
        await Assert.That(threw).IsTrue();
    }
}
