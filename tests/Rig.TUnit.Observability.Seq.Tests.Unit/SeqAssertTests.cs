using Rig.TUnit.Observability.Seq.Assertions;
using Rig.TUnit.Observability.Seq.Fixtures;

namespace Rig.TUnit.Observability.Seq.Tests.Unit;

public sealed class SeqAssertTests
{
    [Test]
    public async Task Query_NullFixture_ThrowsArgumentNullException()
    {
        await Assert.That(() => SeqAssert.Query(null!, "some-filter"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Query_WhitespaceFilter_ThrowsArgumentException()
    {
        var fixture = new SeqFixture();

        await Assert.That(() => SeqAssert.Query(fixture, "   "))
            .ThrowsExactly<ArgumentException>();
    }

    [Test]
    public async Task Count_NegativeExpected_ThrowsArgumentOutOfRangeException()
    {
        var fixture = new SeqFixture();

        await Assert.That(() => SeqAssert.Query(fixture, "level = 'Error'").Count(-1))
            .ThrowsExactly<ArgumentOutOfRangeException>();
    }

    [Test]
    public async Task Within_WithoutCountFirst_ThrowsInvalidOperationException()
    {
        var fixture = new SeqFixture();

        await Assert.That(() => SeqAssert.Query(fixture, "level = 'Error'").Within(TimeSpan.Zero))
            .ThrowsExactly<InvalidOperationException>();
    }
}
