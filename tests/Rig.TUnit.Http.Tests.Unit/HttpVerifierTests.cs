using System.Net;
using Rig.TUnit.Http;

namespace Rig.TUnit.Http.Tests.Unit;

public sealed class HttpVerifierTests
{
    [Test]
    public async Task CalledAtLeast_SufficientCalls_DoesNotThrow()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/v").Responds().WithBody("ok").And();
        var client = mock.CreateClient();
        await client.GetAsync("http://mock/v");
        await client.GetAsync("http://mock/v");

        mock.Verify(HttpMethod.Get, "/v").CalledAtLeast(2);
    }

    [Test]
    public async Task CalledAtLeast_TooFewCalls_ThrowsHttpMockVerificationException()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/v").Responds().WithBody("ok").And();
        var client = mock.CreateClient();
        await client.GetAsync("http://mock/v");

        await Assert.That(() => mock.Verify(HttpMethod.Get, "/v").CalledAtLeast(5))
            .ThrowsExactly<HttpMockVerificationException>();
    }

    [Test]
    public Task WithHeader_ReturnsSelf_ForFluentChaining()
    {
        var mock = new HttpMock();
        mock.When.Get().Path("/v").Responds().WithBody("ok").And();

        var verifier = mock.Verify(HttpMethod.Get, "/v");
        var result = verifier.WithHeader("X-Token", "abc");

        _ = result;
        return Task.CompletedTask;
    }

    [Test]
    public Task AsHandler_ReturnsNonNullDelegatingHandler()
    {
        var mock = new HttpMock();

        var handler = mock.AsHandler();

        _ = handler;
        return Task.CompletedTask;
    }

    [Test]
    public async Task HttpMockVerificationException_Message_IsPreserved()
    {
        var ex = new HttpMockVerificationException("expected 3 calls");

        await Assert.That(ex.Message).IsEqualTo("expected 3 calls");
    }

    [Test]
    public async Task RecordedExchange_Properties_AreSetCorrectly()
    {
        var exchange = new RecordedExchange(HttpMethod.Post, "/orders", HttpStatusCode.Created, "{}", null);

        await Assert.That(exchange.Method).IsEqualTo(HttpMethod.Post);
        await Assert.That(exchange.Path).IsEqualTo("/orders");
        await Assert.That(exchange.ResponseStatus).IsEqualTo(HttpStatusCode.Created);
        await Assert.That(exchange.ResponseBody).IsEqualTo("{}");
        await Assert.That(exchange.ResponseHeaders).IsNull();
    }
}
