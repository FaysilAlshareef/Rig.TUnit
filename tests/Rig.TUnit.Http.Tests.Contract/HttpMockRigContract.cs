using System.Net;

namespace Rig.TUnit.Http.Tests.Contract;

/// <summary>
/// Base contract every HttpMock-driven rig suite inherits. Asserts the mock happy-path
/// invariants.
/// </summary>
public abstract class HttpMockRigContract
{
    protected virtual HttpMock BuildMock() => new();

    [Test]
    public async Task Mock_CreatesClient_WithBaseAddress()
    {
        var mock = BuildMock();
        using var client = mock.CreateClient();

        await Assert.That(client.BaseAddress).IsNotNull();
    }

    [Test]
    public async Task Mock_Client_UsesMockBaseAddress()
    {
        var mock = BuildMock();
        using var client = mock.CreateClient();

        await Assert.That(client.BaseAddress!.Host).IsEqualTo("mock");
    }

    [Test]
    public async Task Mock_RegisteredGetExpectation_IsMatched()
    {
        var mock = BuildMock();
        mock.When.Get().Path("/ping").Responds().WithStatus(HttpStatusCode.OK).And();

        using var client = mock.CreateClient();
        var response = await client.GetAsync("/ping");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }
}
