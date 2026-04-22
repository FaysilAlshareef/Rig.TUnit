using Rig.TUnit.Observability.Seq.Assertions;

namespace Rig.TUnit.Observability.Seq.Tests.Unit;

public sealed class SeqAssertionExceptionTests
{
    [Test]
    public async Task SeqAssertionException_Message_IsPreserved()
    {
        var ex = new SeqAssertionException("filter timed out");

        await Assert.That(ex.Message).IsEqualTo("filter timed out");
    }
}
