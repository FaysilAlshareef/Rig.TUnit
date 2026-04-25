# Planning — mTLS revocation + chain + hostname (F-031)

**Feature ID**: F-031
**Family**: Security
**Status**: planned
**Depends on**: F-008 (deterministic clock — cert expiry)
**Target release**: v0.13
**Estimated tasks**: ~26 (Phase 0: 7 · 1 package × 14 tasks · 5 docs)

---

## Why this feature exists

`Rig.TUnit.Security.Mtls` is currently minimal. Real mTLS testing requires CA / cert generation, revocation simulation, chain-validation tests, and hostname-mismatch scenarios. Today users either skip mTLS in tests (false positives) or hand-roll certificates with `certutil` / OpenSSL (fragile).

Real-world bugs the rig must catch:
- A revoked client cert still accepted because CRL/OCSP isn't checked.
- An expired intermediate-CA cert breaks the chain — the leaf cert fails for an apparently random reason.
- A hostname mismatch (cert SAN says `api.example.com`, request goes to `api2.example.com`) silently accepted.
- Renegotiation mid-stream not handled.

## What we deliver

A CA/cert generation surface and an mTLS attack-scenario API:

```csharp
public sealed class MtlsTopologyBuilder
{
    public CaCertHandle GenerateCA(string subject, TimeSpan validity);
    public ClientCertHandle GenerateClientCert(string clientName, CaCertHandle ca, TimeSpan validity);
    public ServerCertHandle GenerateServerCert(string commonName, CaCertHandle ca, TimeSpan validity, params string[] sans);
    public ClientCertHandle Revoke(ClientCertHandle cert);
}

public sealed class MtlsFixture
{
    public Uri CrlUri { get; }
    public Uri OcspResponderUri { get; }
}

public static class MtlsAssert
{
    public static HandshakeAssertion Handshake();
}

public sealed class HandshakeAssertion
{
    public HandshakeAssertion Failed().WithAlert(TlsAlert alert);
    public HandshakeAssertion Succeeded().WithClientCert(ClientCertHandle cert);
    public HandshakeAssertion ChainBuiltTo(CaCertHandle root);
}
```

## Gaps closed (from SEC-4 in the gap analysis)

- CRL / OCSP revocation testing.
- Chain validation — intermediate-CA expiry, missing intermediate.
- Hostname mismatch / SAN matching.
- Client-cert-required vs optional negotiation.

## Providers in scope

1: `src/Rig.TUnit.Security.Mtls` plus integration-level wiring into `Rig.TUnit.Http` and `Rig.TUnit.Grpc` for mTLS test handlers.

## Exit criteria

- `MtlsTopologyBuilder`, `MtlsFixture`, `MtlsAssert` ship with 100 % line coverage.
- ≥ 5 RED-leading scenarios (revoked-cert rejected, expired-intermediate fails, SAN-mismatch fails, optional-vs-required negotiation, OCSP-stapling).
- `docs/providers/security.md` updated.

## Dependencies on other planned features

- Upstream: F-008.
- Downstream: F-045 (gRPC mTLS handler reuses MtlsFixture).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 031-mtls-revocation-and-chain

Read first:
- planning/mtls-revocation-and-chain/README.md
- planning/deterministic-clock/README.md (F-008 must be shipped)
- src/Rig.TUnit.Security.Mtls/* (current state)
- BouncyCastle .NET cert-generation samples; X509Certificate2.CreateFromCertFile usage

Generate a feature spec that:
1. Introduces MtlsTopologyBuilder + MtlsFixture (CRL + OCSP responders).
2. MtlsAssert.Handshake operators (Failed.WithAlert, Succeeded.WithClientCert, ChainBuiltTo).
3. ≥ 5 RED-leading scenarios.

Constraints:
- Cert validity advanced under F-008's IFakeClock.
- CRL / OCSP responders run in-process, no external services.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
