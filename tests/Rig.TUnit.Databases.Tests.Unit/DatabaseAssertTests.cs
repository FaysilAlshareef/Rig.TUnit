using NSubstitute;
using Rig.TUnit.Databases.Assertions;
using Rig.TUnit.Databases.Contracts;

namespace Rig.TUnit.Databases.Tests.Unit;

public sealed class DatabaseAssertTests
{
    [Test]
    public async Task TableExists_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<ISchemaProbe>();

        await Assert.That(() => DatabaseAssert.TableExists(null!, probe, "table"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task TableExists_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => DatabaseAssert.TableExists(rig, null!, "table"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task TableExists_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<ISchemaProbe>();
        probe.TableExistsAsync(rig, "table", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DatabaseAssert.TableExists(rig, probe, "table");

        await probe.Received(1).TableExistsAsync(rig, "table", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task RowCount_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<ISchemaProbe>();

        await Assert.That(() => DatabaseAssert.RowCount(null!, probe, "table", 5))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RowCount_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => DatabaseAssert.RowCount(rig, null!, "table", 5))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task RowCount_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<ISchemaProbe>();
        probe.RowCountAsync(rig, "table", 5L, Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DatabaseAssert.RowCount(rig, probe, "table", 5);

        await probe.Received(1).RowCountAsync(rig, "table", 5L, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task ColumnType_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<ISchemaProbe>();

        await Assert.That(() => DatabaseAssert.ColumnType(null!, probe, "table", "col", "TEXT"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ColumnType_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => DatabaseAssert.ColumnType(rig, null!, "table", "col", "TEXT"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task ColumnType_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<ISchemaProbe>();
        probe.ColumnTypeAsync(rig, "table", "col", "TEXT", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DatabaseAssert.ColumnType(rig, probe, "table", "col", "TEXT");

        await probe.Received(1).ColumnTypeAsync(rig, "table", "col", "TEXT", Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task IndexExists_NullRig_ThrowsArgumentNullException()
    {
        var probe = Substitute.For<ISchemaProbe>();

        await Assert.That(() => DatabaseAssert.IndexExists(null!, probe, "table", "idx"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IndexExists_NullProbe_ThrowsArgumentNullException()
    {
        var rig = Substitute.For<IDbRig>();

        await Assert.That(() => DatabaseAssert.IndexExists(rig, null!, "table", "idx"))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task IndexExists_ValidArgs_DelegatesToProbe()
    {
        var rig = Substitute.For<IDbRig>();
        var probe = Substitute.For<ISchemaProbe>();
        probe.IndexExistsAsync(rig, "table", "idx", Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        await DatabaseAssert.IndexExists(rig, probe, "table", "idx");

        await probe.Received(1).IndexExistsAsync(rig, "table", "idx", Arg.Any<CancellationToken>());
    }
}
