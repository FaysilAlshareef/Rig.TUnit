# Planning — Storage multipart / streaming / conditional writes (F-028)

**Feature ID**: F-028
**Family**: Storage
**Status**: planned
**Depends on**: F-027 (bucket lifecycle topology)
**Target release**: v0.13
**Estimated tasks**: ~50 (Phase 0: 5 · 4 providers × 10 tasks · 5 docs)

---

## Why this feature exists

Real applications upload large files. The rig today only round-trips small byte buffers via the simplest `PutObject` / `UploadAsync`. Production reality:

- 5 GB files via multipart / block-blob uploads.
- Resume-after-failure for partial uploads.
- Abort-incomplete cleanup (otherwise S3 charges for orphaned parts forever).
- ETag verification on completed uploads.
- Conditional writes (`If-Match` / `If-None-Match`) — concurrent uploaders racing on the same key.
- Eventual-consistency windows (S3 historical, Azure lease conflicts, MinIO erasure-coded write quorum).
- Server-Sent-Event-style streaming uploads / downloads.
- Checksum validation (CRC32C / MD5) as part of the protocol.

Real-world bugs the rig must catch:
- A multipart upload aborting mid-flight with a stale uploadId — orphan parts left behind.
- A conditional `PutObject` with `If-None-Match: *` failing because the key already exists, but the app retried blindly.
- A presigned URL expiring mid-upload — the client must handle a `403` clearly.

## What we deliver

A `MultipartUpload` API on each fixture plus assertions and consistency-window simulation:

```csharp
public sealed class MultipartUploadBuilder
{
    public MultipartUploadBuilder ToKey(string key);
    public MultipartUploadBuilder Parts(int count);
    public MultipartUploadBuilder PartSize(int bytes);
    public MultipartUploadBuilder AbortAt(int partIndex);
    public MultipartUploadBuilder ResumeAfterFailure(bool resume);
    public Task<MultipartUploadResult> ExecuteAsync(CancellationToken ct);
}

public static class StorageAssert
{
    public static ObjectAssertion Object(string key);
    public static ConsistencyAssertion ListAfterPut(string prefix);
}

public sealed class ObjectAssertion
{
    public ObjectAssertion MultipartCompleted();
    public ObjectAssertion IntegrityVerified(); // ETag / checksum
    public ObjectAssertion PutFailed(int statusCode);
    public ObjectAssertion BecauseEtagMismatch();
    public ObjectAssertion HasNoOrphanParts();
}

public abstract partial class StorageFixture
{
    public IDisposable WithConsistencyDelay(TimeSpan span);
}
```

## Gaps closed (from STORE-1 + STORE-2 + STORE-3 in the gap analysis)

- Multipart / streaming uploads.
- Conditional writes (If-Match / If-None-Match).
- Eventual-consistency window simulation.
- Orphan-part detection.

## Providers in scope

4: S3, MinIO, AzureBlob (block blob), FileSystem (atomic-rename simulation).

## Exit criteria

- `MultipartUploadBuilder` and `StorageAssert.Object` ship with 100 % line coverage.
- Each provider has ≥ 4 RED scenarios (multipart success, multipart aborted resumes cleanly, conditional-put fails on race, no orphan parts after abort+cleanup).
- `docs/providers/*.md` updated.

## Dependencies on other planned features

- Upstream: F-027.
- Downstream: F-029 (encryption / object lock / replication).

## Build prompt (paste into `/dai.spec` when picked up)

```
Feature: 028-storage-multipart-and-conditional

Read first:
- planning/storage-multipart-and-conditional/README.md
- planning/storage-bucket-lifecycle-topology/README.md (F-027 must be shipped)
- AWS S3 multipart-upload docs, Azure block-blob docs, MinIO multipart docs

Generate a feature spec that:
1. Introduces MultipartUploadBuilder + StorageAssert.Object + WithConsistencyDelay.
2. Each provider phase ships ≥ 4 RED scenarios.
3. Orphan-part detection runs on fixture teardown.

Constraints:
- F-008 IFakeClock used for any consistency-window assertion.
- No real Task.Delay — consistency-window is fake-clock-driven.
- Pre-release library — no [Obsolete].

Deliverables: spec.md, plan.md, tasks.md, data-model.md, research.md, quickstart.md.
```
