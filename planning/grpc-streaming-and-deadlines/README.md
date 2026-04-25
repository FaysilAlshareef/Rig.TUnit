# Planning — gRPC streaming / deadlines / metadata / retry (F-044)

**Feature ID**: F-044
**Family**: gRPC
**Status**: planned
**Depends on**: F-008 (clock for deadlines)
**Target release**: v0.13
**Estimated tasks**: ~28 (Phase 0: 7 · 1 package × 16 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Grpc` provides minimal mocking. Real gRPC testing requires:

- Server-streaming, client-streaming, bidi streaming with `WriteAsync` / `MoveNext`.
- Deadlines / timeouts (`CallOptions.Deadline`).
- Cancellation propagation client → server → handler.
- Status metadata round-trip — `google.rpc.ErrorInfo`, `RetryInfo`, custom keys.
- Service-config retry policy (`methodConfig.retryPolicy`).
- Half-close ordering on bidi streams.

Real-world bugs the rig must catch:
- A handler that doesn't honour `context.CancellationToken` — wastes resources after deadline exceeded.
- An interceptor that drops metadata.
- Retry policy that retries non-idempotent methods.

## What we deliver

```csharp
public sealed class GrpcMockServer
{
    public Uri Endpoint { get; }
    public GrpcMockServer ServiceImpl<TService>(TService impl);
    public GrpcMockServer Interceptor(Interceptor interceptor);
}

public abstract partial class GrpcRigBuilder
{
    public GrpcRigBuilder WithGrpcServer(Action<GrpcMockServer> configure);
}

public static class GrpcAssert
{
    public static GrpcCallAssertion Call(string methodName);
    public static GrpcStreamAssertion Stream(string methodName);
    public static GrpcRetryAssertion RetryAttempts(string methodName);
}

public sealed class GrpcCallAssertion
{
    public GrpcCallAssertion DeadlineExceeded();
    public GrpcCallAssertion CancelledFromClient();
    public GrpcCallAssertion MetadataKey(string key).Value(string expected);
    public GrpcCallAssertion StatusCode(StatusCode code).WithDetails<T>();
}

public sealed class GrpcStreamAssertion
{
    public GrpcStreamAssertion ReceivedItems(int n);
    public GrpcStreamAssertion ClosedWith(StatusCode code);
    public GrpcStreamAssertion HalfClosedClient();
    public GrpcStreamAssertion HalfClosedServer();
}

public sealed class GrpcRetryAssertion
{
    public GrpcRetryAssertion Count(int expected);
    public GrpcRetryAssertion BackoffMatches(BackoffShape shape);
}
```

## Gaps closed (from GRPC-1 in the gap analysis)

- Streaming RPC semantics.
- Deadline / cancellation propagation.
- Retry policy via service config.
- Status metadata round-trip.

## Providers in scope

1: `src/Rig.TUnit.Grpc`.

## Exit criteria

- `GrpcMockServer`, `GrpcAssert.Call / Stream / RetryAttempts` ship with 100 % line coverage.
- ≥ 6 RED scenarios (deadline exceeded, cancellation propagation, server-stream close, client-stream half-close, retry-with-jitter, status-detail round-trip).
- F-008 fake-clock used for deadline assertions.
- `docs/providers/grpc.md` updated.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-045 (mTLS handler reuses GrpcMockServer).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 044-grpc-streaming-and-deadlines

Read first:
- planning/grpc-streaming-and-deadlines/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- src/Rig.TUnit.Grpc/* (current state)
- gRPC service-config docs (https://github.com/grpc/grpc-proto/blob/master/grpc/service_config/service_config.proto)

Generate a feature spec that:
1. Introduces GrpcMockServer + WithGrpcServer on RigBuilder.
2. GrpcAssert.Call / Stream / RetryAttempts.
3. ≥ 6 RED scenarios.

Constraints:
- F-008 IFakeClock for deadlines.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
