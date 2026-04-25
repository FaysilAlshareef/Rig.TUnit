# Planning — HTTP streaming / SSE / WebSocket / HTTP2 (F-042)

**Feature ID**: F-042
**Family**: HTTP
**Status**: planned
**Depends on**: F-009 (chaos — mid-stream disconnects)
**Target release**: v0.13
**Estimated tasks**: ~28 (Phase 0: 7 · 1 package × 16 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Http/HttpMock.cs` matches request-response. Modern HTTP work needs streaming:

- **Chunked transfer-encoding** — partial responses.
- **Server-Sent Events (SSE)** — `text/event-stream` with `data:` frames, retry timers.
- **WebSocket upgrade** — handshake, frames, ping/pong, close codes.
- **HTTP/2 multiplexing** — multiple concurrent streams on one connection, GOAWAY handling.
- **HTTP/3 / QUIC** (deferred — flag as out-of-scope unless .NET tooling matures).

Real-world bugs the rig must catch:
- An SSE client that doesn't honour `retry: 5000`.
- A WebSocket client that doesn't send pings → idle timeout.
- A server that GOAWAYs mid-stream → client must redrive the in-flight requests.

## What we deliver

A streaming-aware extension of `HttpMock` plus assertions:

```csharp
public abstract partial class HttpFixture
{
    public IServerSentEventsClient ServerSentEvents(Uri uri, Action<ISseClientConfig>? configure = null);
    public IWebSocketClient WebSocket(Uri uri, Action<IWebSocketClientConfig>? configure = null);
    public IHttp2MultiplexClient Http2(Uri uri);
}

public static class HttpAssert
{
    public static SseAssertion Sse(IServerSentEventsClient client);
    public static WebSocketAssertion Ws(IWebSocketClient client);
    public static Http2Assertion Http2(IHttp2MultiplexClient client);
}

public sealed class SseAssertion
{
    public SseAssertion ReceivedEvents(int count);
    public SseAssertion EventOfType(string type).Count(int n);
    public SseAssertion ReconnectedAfter(TimeSpan span);
}

public sealed class WebSocketAssertion
{
    public WebSocketAssertion FramesReceived(int count);
    public WebSocketAssertion ClosedWith(WebSocketCloseStatus status);
    public WebSocketAssertion PingPongRoundTrips(int min);
}
```

## Gaps closed (from HTTP-1 in the gap analysis)

- SSE / WebSocket / HTTP/2 streaming.
- Mid-stream disconnect / GOAWAY handling.
- Pings / keepalives.

## Providers in scope

1: `src/Rig.TUnit.Http`.

## Exit criteria

- `IServerSentEventsClient`, `IWebSocketClient`, `IHttp2MultiplexClient` ship with 100 % line coverage.
- ≥ 6 RED scenarios (SSE retry, WebSocket close codes, HTTP/2 GOAWAY, mid-stream disconnect, ping/pong, multiplex stream contention).
- `docs/providers/http.md` updated.

## Dependencies on other planned features

- Upstream: F-009 (chaos for mid-stream disconnect injection).
- Downstream: F-043 (cookies / redirects / CORS / negotiation).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 042-http-streaming-and-protocols

Read first:
- planning/http-streaming-and-protocols/README.md
- planning/fault-and-chaos-injection/README.md (F-009 must be shipped)
- src/Rig.TUnit.Http/* (current state)
- HTML SSE spec, RFC 6455 (WebSocket), RFC 9113 (HTTP/2)

Generate a feature spec that:
1. Introduces ServerSentEvents / WebSocket / Http2 client surfaces on HttpFixture.
2. HttpAssert.Sse / Ws / Http2 with frame / event / multiplex operators.
3. ≥ 6 RED scenarios.

Constraints:
- All clients fault-injection-friendly (F-009 plumbing).
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
