using System.Net;
using Rig.TUnit.Http;

namespace Rig.TUnit.Http.Tests.Unit;

public sealed class HttpRequestBuilderExtendedTests
{
    // ─── HTTP method shortcuts ────────────────────────────────────────────────

    [Test]
    public async Task Put_MatchesOnPutMethod()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Put().Path("/resource").Responds().WithBody("updated").And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.PutAsync("http://mock/resource", new StringContent(string.Empty));

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("updated");
    }

    [Test]
    public async Task Delete_MatchesOnDeleteMethod()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Delete().Path("/resource").Responds().WithStatus(HttpStatusCode.NoContent).And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.DeleteAsync("http://mock/resource");

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NoContent);
    }

    [Test]
    public async Task Patch_MatchesOnPatchMethod()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Patch().Path("/resource").Responds().WithBody("patched").And();

        // Act
        var client = mock.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Patch, "http://mock/resource");
        var resp = await client.SendAsync(req);

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("patched");
    }

    // ─── BodyContains matcher ─────────────────────────────────────────────────

    [Test]
    public async Task BodyContains_MatchingFragment_ReturnsConfiguredResponse()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Post().Path("/data").BodyContains("hello").Responds().WithBody("matched").And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.PostAsync("http://mock/data", new StringContent("say hello world"));

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("matched");
    }

    [Test]
    public async Task BodyContains_MissingFragment_ReturnsNotFound()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Post().Path("/data").BodyContains("hello").Responds().WithBody("matched").And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.PostAsync("http://mock/data", new StringContent("goodbye"));

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    // ─── Response configurator ────────────────────────────────────────────────

    [Test]
    public async Task WithJson_SetsContentTypeToApplicationJson()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Get().Path("/data").Responds().WithJson("{\"key\":\"value\"}").And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/data");

        // Assert
        await Assert.That(resp.Content.Headers.ContentType?.MediaType).IsEqualTo("application/json");
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("{\"key\":\"value\"}");
    }

    [Test]
    public async Task WithJitter_Configured_RequestSucceeds()
    {
        // Arrange — small jitter to exercise the Jitter path in the handler
        var mock = new HttpMock();
        mock.When.Get().Path("/slow").Responds().WithBody("ok").WithJitter(TimeSpan.FromMilliseconds(5)).And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/slow");

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
    }

    [Test]
    public async Task WithHeader_ResponseContainsConfiguredHeader()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Get().Path("/api")
            .Responds()
            .WithBody("body")
            .WithHeader("X-Correlation-Id", "abc123")
            .And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/api");

        // Assert
        await Assert.That(resp.Headers.GetValues("X-Correlation-Id").FirstOrDefault())
            .IsEqualTo("abc123");
    }

    // ─── Responds(status, body, headers) overload ────────────────────────────

    [Test]
    public async Task Responds_WithStatusBodyAndHeaders_ConfiguresAllThree()
    {
        // Arrange
        var mock = new HttpMock();
        mock.When.Get().Path("/api")
            .Responds(HttpStatusCode.Accepted, "accepted-body",
                new Dictionary<string, string> { ["X-Custom"] = "value123" })
            .And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/api");

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Accepted);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("accepted-body");
        await Assert.That(resp.Headers.GetValues("X-Custom").FirstOrDefault()).IsEqualTo("value123");
    }

    // ─── ParseQuery no-equals branch ─────────────────────────────────────────

    [Test]
    public async Task ParseQuery_KeyWithNoEqualsSign_TreatsValueAsEmpty()
    {
        // Arrange — ?flag has no = so the value should be empty string
        var mock = new HttpMock();
        mock.When.Get().Path("/search")
            .Query("flag", string.Empty)
            .Responds().WithBody("found").And();

        // Act
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/search?flag");

        // Assert
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("found");
    }
}
