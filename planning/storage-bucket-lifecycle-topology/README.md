# Planning — Storage bucket-lifecycle topology (F-027)

**Feature ID**: F-027
**Family**: Storage
**Status**: planned
**Depends on**: —
**Target release**: v0.12
**Estimated tasks**: ~70 (Phase 0: 7 · 4 providers × 14 tasks · 7 docs/bench)

---

## Why this feature exists

Storage builders today expose connection wiring only:
- `src/Rig.TUnit.Storage.S3/Builder/S3RigBuilder.cs` — endpoint and credentials.
- `src/Rig.TUnit.Storage.MinIO/Builder/MinIORigBuilder.cs` — same shape.
- `src/Rig.TUnit.Storage.AzureBlob/Builder/AzureBlobRigBuilder.cs` — connection string.
- `src/Rig.TUnit.Storage.FileSystem/Builder/*` — root path.

There is no fluent surface to declare buckets / containers, lifecycle rules, versioning, replication, CORS, presigned-URL policies, MFA delete, encryption keys, object lock, immutability, blob leases, or paths.

This is the **storage analogue of Feature 007's `WithTopology`**.

## What we deliver

A `WithTopology(Action<I{Provider}StorageTopologyBuilder>)` builder method per storage provider. Per-provider sub-interfaces hold only operations the engine supports.

```csharp
public interface IS3TopologyBuilder : ITopologyBuilder
{
    IS3TopologyBuilder Bucket(string name, Action<IS3BucketConfig>? configure = null);
}

public interface IS3BucketConfig
{
    IS3BucketConfig WithVersioning(bool enabled = true);
    IS3BucketConfig WithLifecycle(Action<ILifecycleRulesBuilder> rules);
    IS3BucketConfig WithCors(params CorsRule[] rules);
    IS3BucketConfig WithObjectLock(ObjectLockMode mode, TimeSpan retention);
    IS3BucketConfig WithReplicationTo(string destinationArn, string? roleArn = null);
}

public interface IAzureBlobTopologyBuilder : ITopologyBuilder
{
    IAzureBlobTopologyBuilder Container(string name, Action<IAzureBlobContainerConfig>? configure = null);
}

public interface IAzureBlobContainerConfig
{
    IAzureBlobContainerConfig WithImmutability(TimeSpan retention);
    IAzureBlobContainerConfig WithLease(TimeSpan duration);
    IAzureBlobContainerConfig WithPublicAccess(PublicAccessType level);
}

public interface IFileSystemTopologyBuilder : ITopologyBuilder
{
    IFileSystemTopologyBuilder Path(string root, Action<IFileSystemPathConfig>? configure = null);
}
```

## Gaps closed (from STORE-* in the gap analysis, the lifecycle subset)

- Bucket / container creation.
- Versioning / lifecycle / replication / CORS / object-lock topology.
- File-system root-path declaration with permission / watch config.

## Providers in scope

4: S3, MinIO, AzureBlob, FileSystem.

## Exit criteria

- `ITopologyBuilder` (already from Feature 007) reused as the marker.
- 4 provider sub-interfaces ship with 100 % line coverage in introducing PRs.
- `ProviderCompletenessTests` extended with `StorageProviders_Declare_WithTopology` rule, parity coverage file.
- Each provider has ≥ 4 RED scenarios (bucket create, lifecycle rule, CORS, idempotent re-apply).
- `docs/providers/{s3,minio,azureblob,filesystem}.md` updated.

## Dependencies on other planned features

- Upstream: none (Feature 007 marker reused).
- Downstream: F-028 (multipart / streaming / conditional writes), F-029 (encryption / replication / object lock — deepens this feature).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 027-storage-bucket-lifecycle-topology

Read first:
- planning/storage-bucket-lifecycle-topology/README.md
- .dotnet-ai-kit/features/007-messaging-topology-sessions/spec.md (analogue pattern)
- AWS S3, Azure Blob, MinIO admin SDK docs

Generate a feature spec that:
1. Reuses ITopologyBuilder marker from Feature 007.
2. Adds 4 provider-scoped sub-interfaces with WithTopology on each StorageRigBuilder.
3. Phase 0 lands ProviderCompletenessTests parity rule.
4. Each provider phase ships ≥ 4 RED scenarios.

Constraints:
- ApplyAsync idempotent (re-create-existing → no-op).
- No shared "WithVersioning" — each provider's own surface, no fake polyfills (FileSystem doesn't have versioning).
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
