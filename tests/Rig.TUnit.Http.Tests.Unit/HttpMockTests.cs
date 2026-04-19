using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Rig.TUnit.Http;

namespace Rig.TUnit.Http.Tests.Unit;

public sealed class HttpMockTests
{
    // ─── Matcher matrix ──────────────────────────────────────────────────────

    [Test]
    public async Task MethodAndPath_Match_ReturnsConfiguredBody()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/hello").Responds().WithBody("world").And();

        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/hello");
        var body = await resp.Content.ReadAsStringAsync();

        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(body).IsEqualTo("world");
    }

    [Test]
    public async Task PathRegex_Match()
    {
        var mock = new HttpMock();
        mock.When.Get().PathMatching("^/items/\\d+$").Responds().WithBody("item").And();

        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/items/42");

        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("item");
    }

    [Test]
    public async Task NoMatch_ReturnsNotFound()
    {
        var mock = new HttpMock();
        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/nothing");
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task Query_Matcher_Filters()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/search").Query("q", "foo").Responds().WithBody("foo-results").And();
        mock.When.Get().Path("/search").Responds().WithBody("no-q").And();

        var client = mock.CreateClient();
        var a = await (await client.GetAsync("http://mock/search?q=foo")).Content.ReadAsStringAsync();
        var b = await (await client.GetAsync("http://mock/search?q=bar")).Content.ReadAsStringAsync();

        await Assert.That(a).IsEqualTo("foo-results");
        await Assert.That(b).IsEqualTo("no-q");
    }

    [Test]
    public async Task Header_Matcher_Filters()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/me").Header("X-Tenant", "acme").Responds().WithBody("acme").And();

        var client = mock.CreateClient();
        var req = new HttpRequestMessage(HttpMethod.Get, "http://mock/me");
        req.Headers.Add("X-Tenant", "acme");
        var resp = await client.SendAsync(req);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("acme");
    }

    [Test]
    public async Task JsonPath_Matcher_Filters()
    {
        var mock = new HttpMock();
        mock.When.Post().Path("/orders").JsonPathEquals("customer.id", "c-1").Responds().WithStatus(HttpStatusCode.Created).WithBody("created").And();

        var client = mock.CreateClient();
        var content = new StringContent("{\"customer\":{\"id\":\"c-1\"}}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync("http://mock/orders", content);
        await Assert.That(resp.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    [Test]
    public async Task BodyRegex_Matcher_Filters()
    {
        var mock = new HttpMock();
        mock.When.Post().Path("/events").BodyMatching("evt-\\d+").Responds().WithBody("ok").And();

        var client = mock.CreateClient();
        var content = new StringContent("{\"id\":\"evt-123\"}", Encoding.UTF8, "application/json");
        var resp = await client.PostAsync("http://mock/events", content);
        await Assert.That(await resp.Content.ReadAsStringAsync()).IsEqualTo("ok");
    }

    // ─── Scenario state machine ──────────────────────────────────────────────

    [Test]
    public async Task Scenario_ReturnsSequentialResponses_ThenSticks()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/seq").Responds().WithBody("first").Then().WithBody("second").Then().WithBody("third").And();

        var client = mock.CreateClient();
        var r1 = await (await client.GetAsync("http://mock/seq")).Content.ReadAsStringAsync();
        var r2 = await (await client.GetAsync("http://mock/seq")).Content.ReadAsStringAsync();
        var r3 = await (await client.GetAsync("http://mock/seq")).Content.ReadAsStringAsync();
        var r4 = await (await client.GetAsync("http://mock/seq")).Content.ReadAsStringAsync();

        await Assert.That(r1).IsEqualTo("first");
        await Assert.That(r2).IsEqualTo("second");
        await Assert.That(r3).IsEqualTo("third");
        await Assert.That(r4).IsEqualTo("third");
    }

    // ─── Delay / intermittent failure ────────────────────────────────────────

    [Test]
    public async Task Delay_RespectedByResponse()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/slow").Responds().WithBody("ok").WithDelay(TimeSpan.FromMilliseconds(200)).And();

        var client = mock.CreateClient();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        await client.GetAsync("http://mock/slow");
        sw.Stop();

        await Assert.That(sw.ElapsedMilliseconds).IsGreaterThanOrEqualTo(180);
    }

    [Test]
    public async Task IntermittentFailure_Every2_Returns503OnEvenCalls()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/f").Responds().WithBody("ok").IntermittentFailure(every: 2).And();

        var client = mock.CreateClient();
        var r1 = await client.GetAsync("http://mock/f");
        var r2 = await client.GetAsync("http://mock/f");
        var r3 = await client.GetAsync("http://mock/f");
        var r4 = await client.GetAsync("http://mock/f");

        await Assert.That(r1.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(r2.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
        await Assert.That(r3.StatusCode).IsEqualTo(HttpStatusCode.OK);
        await Assert.That(r4.StatusCode).IsEqualTo(HttpStatusCode.ServiceUnavailable);
    }

    // ─── Binary / SSE ────────────────────────────────────────────────────────

    [Test]
    public async Task Binary_Response_Returns_Bytes()
    {
        var mock = new HttpMock();
        var bytes = new byte[] { 1, 2, 3, 4, 5 };
        mock.When.Get().Path("/img").Responds().WithBinary(bytes, "image/png").And();

        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/img");
        var returned = await resp.Content.ReadAsByteArrayAsync();

        await Assert.That(returned).IsEquivalentTo(bytes);
        await Assert.That(resp.Content.Headers.ContentType!.MediaType).IsEqualTo("image/png");
    }

    [Test]
    public async Task Sse_Response_SetsEventStreamHeader()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/stream").Responds().WithSse("data: one\n\ndata: two\n\n").And();

        var client = mock.CreateClient();
        var resp = await client.GetAsync("http://mock/stream");

        await Assert.That(resp.Content.Headers.ContentType!.MediaType).IsEqualTo("text/event-stream");
        await Assert.That(await resp.Content.ReadAsStringAsync()).Contains("data: one");
    }

    // ─── Record / replay ─────────────────────────────────────────────────────

    [Test]
    public async Task Replay_From_Recorded_Exchanges()
    {
        var mock = new HttpMock();
        mock.ReplayFrom(new[]
        {
            new RecordedExchange(HttpMethod.Get, "/a", HttpStatusCode.OK, "alpha"),
            new RecordedExchange(HttpMethod.Get, "/b", HttpStatusCode.Created, "beta"),
        });

        var client = mock.CreateClient();
        var a = await client.GetAsync("http://mock/a");
        var b = await client.GetAsync("http://mock/b");

        await Assert.That(await a.Content.ReadAsStringAsync()).IsEqualTo("alpha");
        await Assert.That(b.StatusCode).IsEqualTo(HttpStatusCode.Created);
    }

    // ─── Verify ──────────────────────────────────────────────────────────────

    [Test]
    public async Task Verify_Called_ExactCount()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/v").Responds().WithBody("ok").And();

        var client = mock.CreateClient();
        await client.GetAsync("http://mock/v");
        await client.GetAsync("http://mock/v");

        mock.Verify(HttpMethod.Get, "/v").Called(2);
    }

    [Test]
    public async Task Verify_Called_WithWrongCount_Throws()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/v").Responds().WithBody("ok").And();
        var client = mock.CreateClient();
        await client.GetAsync("http://mock/v");

        var threw = false;
        try { mock.Verify(HttpMethod.Get, "/v").Called(5); }
        catch (HttpMockVerificationException) { threw = true; }
        await Assert.That(threw).IsTrue();
    }
}
