using System.Net;

namespace Rig.TUnit.Http.Tests.Integration;

/// <summary>
/// End-to-end exercises of <see cref="HttpMock"/> — matches, canned responses, and
/// default-unmatched behaviour for an <see cref="HttpClient"/> sourced from the mock.
/// </summary>
public sealed class HttpMockIntegrationTests
{
    [Test]
    public async Task HttpMock_MatchesGet_AndReturnsConfiguredStatus()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/api/things").Responds().WithStatus(HttpStatusCode.OK).WithJson("{\"ok\":true}").And();

        using var client = mock.CreateClient();
        var response = await client.GetAsync("/api/things");

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).Contains("\"ok\":true");
    }

    [Test]
    public async Task HttpMock_MatchesPost_AndReturnsConfiguredStatus()
    {
        var mock = new HttpMock();
        mock.When.Post().Path("/api/widgets").Responds().WithStatus(HttpStatusCode.Created).And();

        using var client = mock.CreateClient();
        var response = await client.PostAsync("/api/widgets", new StringContent("body"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task HttpMock_CreatesClient_WithExpectedBaseAddress()
    {
        var mock = new HttpMock();
        using var client = mock.CreateClient();

        await Assert.That(client.BaseAddress).IsNotNull();
        await Assert.That(client.BaseAddress!.ToString()).StartsWith("http://mock/");
    }

    [Test]
    public async Task HttpMock_AsHandler_CanBePluggedIntoExistingPipeline()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/ping").Responds().WithStatus(HttpStatusCode.OK).And();

        using var handler = mock.AsHandler();
        using var client = new HttpClient(handler) { BaseAddress = new Uri("http://mock/") };

        var response = await client.GetAsync("/ping");
        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task HttpMock_BodyContains_MatchesRequestPayload()
    {
        var mock = new HttpMock();
        mock.When.Post().Path("/api/echo").BodyContains("hello")
            .Responds().WithStatus(HttpStatusCode.OK).WithBody("matched").And();

        using var client = mock.CreateClient();
        var response = await client.PostAsync("/api/echo", new StringContent("say hello world"));

        await Assert.That(response.StatusCode).IsEqualTo(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        await Assert.That(body).IsEqualTo("matched");
    }
}
