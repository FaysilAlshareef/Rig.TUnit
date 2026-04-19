using Microsoft.Extensions.Options;
using Rig.TUnit.Databases.NoSql.Dynamo.Fixtures;
using Rig.TUnit.Databases.NoSql.Dynamo.Options;

namespace Rig.TUnit.Databases.NoSql.Dynamo.Tests.Unit;

public sealed class DynamoFixtureTests
{
    [Test]
    public async Task Ctor_Parameterless_DoesNotThrow()
    {
        await Assert.That(() => new DynamoFixture()).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptions_DoesNotThrow()
    {
        var options = new DynamoFixtureOptions { ImageTag = "3" };
        await Assert.That(() => new DynamoFixture(options)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithDirectOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new DynamoFixture((DynamoFixtureOptions)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task Ctor_WithIOptions_DoesNotThrow()
    {
        var wrapped = Microsoft.Extensions.Options.Options.Create(new DynamoFixtureOptions());
        await Assert.That(() => new DynamoFixture(wrapped)).ThrowsNothing();
    }

    [Test]
    public async Task Ctor_WithIOptionsNull_ThrowsArgumentNullException()
    {
        await Assert.That(() => new DynamoFixture((IOptions<DynamoFixtureOptions>)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ConnectionString_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new DynamoFixture();
        await Assert.That(() => { _ = fx.ConnectionString; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task Client_BeforeInitialize_ThrowsInvalidOperation()
    {
        var fx = new DynamoFixture();
        await Assert.That(() => { _ = fx.Client; })
            .ThrowsExactly<InvalidOperationException>();
    }

    [Test]
    public async Task DatabaseName_BeforeInitialize_ReturnsStableNonEmptyValue()
    {
        var fx = new DynamoFixture();
        var first = fx.DatabaseName;
        var second = fx.DatabaseName;
        await Assert.That(first).IsNotNullOrEmpty();
        await Assert.That(first).IsEqualTo(second);
    }

    [Test]
    public async Task DisposeAsync_BeforeInitialize_IsSafe()
    {
        var fx = new DynamoFixture();
        await Assert.That(async () => await fx.DisposeAsync()).ThrowsNothing();
    }
}
