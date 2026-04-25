# Planning — gRPC reconnection + compression + mTLS handler (F-045)

**Feature ID**: F-045
**Family**: gRPC
**Status**: planned
**Depends on**: F-031 (mTLS revocation), F-044 (gRPC mock server)
**Target release**: v0.14
**Estimated tasks**: ~22 (Phase 0: 5 · 1 package × 12 tasks · 5 docs)

---

## Why this feature exists

After F-044 (streaming / deadlines / retry), gRPC still has:

- **Reconnection / NameResolver refresh** — DNS rotates, SRV records change; the client must rebind without restart.
- **Subchannel keepalive** — HTTP/2 ping / `keepalive_time_ms`.
- **Compression negotiation** per call (`grpc-encoding: gzip` / `identity`).
- **mTLS** integrated with `Rig.TUnit.Security.Mtls` from F-031.

Real-world bugs:
- Client pinned to a stale IP because NameResolver isn't refreshed.
- `keepalive_time_ms` too aggressive → server `GOAWAY` flood.
- Compression mismatch → `Internal: Decompression error`.

## What we deliver

```csharp
public abstract partial class GrpcRigBuilder
{
    public GrpcRigBuilder WithReconnectionPolicy(Action<IReconnectionConfig> configure);
    public GrpcRigBuilder WithCompression(string algorithm); // "gzip" | "identity"
    public GrpcRigBuilder WithMtls(MtlsFixture fixture, ClientCertHandle clientCert);
}

public static class GrpcAssert
{
    public static GrpcReconnectionAssertion Reconnection(GrpcRigBuilder builder);
    public static GrpcCompressionAssertion Compression(string methodName);
    public static GrpcMtlsAssertion Mtls(GrpcRigBuilder builder);
}

public sealed class GrpcReconnectionAssertion
{
    public GrpcReconnectionAssertion ReboundAfter(TimeSpan span).WithoutFailedCalls();
    public GrpcReconnectionAssertion KeepaliveSent(int min);
}

public sealed class GrpcCompressionAssertion
{
    public GrpcCompressionAssertion Algorithm(string expected);
    public GrpcCompressionAssertion FallbackToIdentity();
}
```

## Gaps closed (from GRPC-2 in the gap analysis)

- NameResolver refresh.
- Keepalive ping correctness.
- Compression negotiation per call.
- mTLS test handler integration.

## Providers in scope

1: `src/Rig.TUnit.Grpc`.

## Exit criteria

- `WithReconnectionPolicy`, `WithCompression`, `WithMtls` ship with 100 % line coverage.
- ≥ 5 RED scenarios.
- `docs/providers/grpc.md` updated.

## Dependencies on other planned features

- Upstream: F-031, F-044.
- Downstream: none.

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 045-grpc-reconnection-and-mtls

Read first:
- planning/grpc-reconnection-and-mtls/README.md
- planning/mtls-revocation-and-chain/README.md (F-031 must be shipped)
- planning/grpc-streaming-and-deadlines/README.md (F-044 must be shipped)

Generate a feature spec that:
1. Introduces WithReconnectionPolicy + WithCompression + WithMtls on GrpcRigBuilder.
2. GrpcAssert.Reconnection / Compression / Mtls.
3. ≥ 5 RED scenarios.

Constraints:
- mTLS reuses MtlsFixture from F-031, never duplicates cert generation.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
