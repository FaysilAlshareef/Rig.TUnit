# Planning — Storage SSE-KMS / object-lock / replication / lifecycle (F-029)

**Feature ID**: F-029
**Family**: Storage
**Status**: planned
**Depends on**: F-027 (bucket lifecycle topology)
**Target release**: v0.14
**Estimated tasks**: ~60 (Phase 0: 7 · 4 providers × 12 tasks · 5 docs)

---

## Why this feature exists

After F-027 (bucket topology) and F-028 (multipart / conditional), what remains are the **enterprise** storage features that compliance teams require but the rig still cannot exercise:

- **Server-side encryption** — SSE-S3, SSE-KMS, SSE-C (customer key) on AWS; CMK on Azure.
- **Object lock / immutability / WORM** — retention modes (Governance, Compliance), legal hold.
- **Cross-region replication** — async replication delay, RPO assertions.
- **Lifecycle transitions** — IA → Glacier → Deep Archive, expire-after rules.
- **Object tagging** — used for cost allocation; replicated subject to tag-replication settings.
- **Customer-managed KMS key rotation** — decrypt-failure on rotated key.

## What we deliver

A KMS fixture plus extensions to F-027's bucket configs:

```csharp
public abstract partial class StorageFixture
{
    public IKmsFixture WithKms(Action<IKmsFixtureConfig>? configure = null);
}

public interface IKmsFixture
{
    Task<KmsKeyHandle> CreateKeyAsync(string alias, CancellationToken ct);
    Task RotateKeyAsync(KmsKeyHandle key, CancellationToken ct);
    Task RevokeKeyAsync(KmsKeyHandle key, CancellationToken ct);
}

public static class EncryptionAssert
{
    public static EncryptionAssertion Object(string key);
}

public sealed class EncryptionAssertion
{
    public EncryptionAssertion EncryptedWith(KmsKeyHandle key);
    public EncryptionAssertion DecryptFailedAfterRevoke();
}

public static class LifecycleAssert
{
    public static TransitionAssertion Object(string key);
}

public sealed class TransitionAssertion
{
    public TransitionAssertion TransitionedTo(StorageClass storageClass).After(TimeSpan span);
    public TransitionAssertion ExpiredAfter(TimeSpan span);
}

public static class ReplicationAssert
{
    public static ReplicationAssertion Replicated(string key);
}

public sealed class ReplicationAssertion
{
    public ReplicationAssertion ToRegion(string region).Within(TimeSpan rpo);
}
```

## Gaps closed (from STORE-5, STORE-6 in the gap analysis)

- SSE-KMS / SSE-C / customer-key rotation.
- Object lock / legal hold / retention modes.
- Cross-region replication RPO assertion.
- Lifecycle transitions (cost-tier movement).
- Object tagging and tag-replication.

## Providers in scope

4: S3, MinIO (limited replication), AzureBlob (CMK + immutability), FileSystem (encryption-at-rest via OS).

## Exit criteria

- `IKmsFixture`, `EncryptionAssert`, `LifecycleAssert`, `ReplicationAssert` ship with 100 % line coverage.
- Each provider has ≥ 4 RED scenarios per its supported subset (S3: full set; MinIO/Azure: subset; FileSystem: minimal).
- F-008 fake-clock advances lifecycle transitions deterministically.
- `docs/providers/*.md` updated.

## Dependencies on other planned features

- Upstream: F-008 (clock for lifecycle transitions), F-027.
- Downstream: F-033 (secrets/PII leak detection — KMS keys belong to the security narrative).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 029-storage-encryption-and-replication

Read first:
- planning/storage-encryption-and-replication/README.md
- planning/storage-bucket-lifecycle-topology/README.md (F-027 must be shipped)
- planning/deterministic-clock/README.md (F-008 must be shipped)
- AWS S3 SSE / Object Lock / Lifecycle / Replication docs

Generate a feature spec that:
1. Introduces IKmsFixture + EncryptionAssert + LifecycleAssert + ReplicationAssert.
2. Each provider phase ships ≥ 4 RED scenarios per supported subset.
3. F-008 fake-clock advances lifecycle and replication assertions.

Constraints:
- KMS fixture isolated per test — no shared keys.
- Lifecycle transitions never consume real wall-clock.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
