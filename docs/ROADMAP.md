# Rig.TUnit Roadmap

**Maintainer**: Faysil Alshareef
**Last updated**: 2026-04-25
**Numbering scheme**: see [ADR-009](adr/ADR-009-feature-numbering-after-007.md)
**Source-of-truth rule**: this file lists every feature; per-feature briefs live in [`planning/<slug>/README.md`](../planning/); when a feature is picked up, its full SDD spec is scaffolded under `.dotnet-ai-kit/features/NNN-<slug>/` via `/dai.spec`.

---

## Status legend

| Status | Meaning | Lives in |
|--------|---------|----------|
| `planned` | Brief exists; not yet picked up | `planning/<slug>/README.md` |
| `specced` | `/dai.spec` ran; spec.md exists | `.dotnet-ai-kit/features/NNN-<slug>/spec.md` |
| `in-progress` | Branch open, RED/GREEN commits flowing | `feat/NNN-<slug>` |
| `verifying` | Tasks done, awaiting `/dai.verify` and review | `.dotnet-ai-kit/features/NNN-<slug>/verify.md` |
| `shipped` | Merged to `master`, CHANGELOG entry landed | tag + CHANGELOG.md |

---

## Shipped (history)

| ID    | Title                              | Status   | Lives in                                                     |
|-------|------------------------------------|----------|--------------------------------------------------------------|
| F-001 | Rig.TUnit base library             | shipped  | `.dotnet-ai-kit/features/001-rig-tunit-library/`             |
| F-002 | Fluent-builder expansion           | shipped  | `.dotnet-ai-kit/features/002-rig-tunit-fluent-builder-expansion/` |
| F-003 | Ecosystem expansion (~50 packages) | shipped  | `.dotnet-ai-kit/features/003-rig-tunit-ecosystem-expansion/` |
| F-004 | Provider-consistency remediation   | shipped  | `.dotnet-ai-kit/features/004-provider-consistency-remediation/` |
| F-005 | Legacy coverage & docs parity      | shipped  | `.dotnet-ai-kit/features/005-legacy-coverage-and-docs-parity/` |
| F-006 | Coverage-quality uplift            | shipped  | `.dotnet-ai-kit/features/006-coverage-quality-uplift/`       |
| F-007 | Messaging topology & sessions      | in-progress (Phase 6) | `.dotnet-ai-kit/features/007-messaging-topology-sessions/` |

---

## Planned — Cross-cutting foundations (must ship first)

These features add base-library primitives that downstream features depend on. Implementing them in order minimises rework.

| ID    | Title                              | Family       | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|--------------|---------|------------|--------|-------|-------|
| F-008 | Deterministic clock (`TimeProvider` + `IFakeClock`) | Cross-cut | planned | —          | v0.8   | ~56   | [link](../planning/deterministic-clock/README.md) |
| F-009 | Fault & chaos injection            | Cross-cut    | planned | F-008      | v0.9   | ~108  | [link](../planning/fault-and-chaos-injection/README.md) |
| F-010 | Seed-data factories (`Bogus` integration) | Cross-cut | planned | —          | v0.9   | ~84   | [link](../planning/seed-data-factories/README.md) |
| F-011 | Snapshot / restore between tests   | Cross-cut    | planned | F-010      | v0.10  | ~84   | [link](../planning/snapshot-and-restore/README.md) |
| F-012 | Cross-fixture correlation (W3C trace propagation) | Cross-cut | planned | —          | v0.10  | ~17   | [link](../planning/cross-fixture-correlation/README.md) |
| F-013 | Multi-tenant scope                 | Cross-cut    | planned | F-012      | v0.11  | ~68   | [link](../planning/multi-tenant-scope/README.md) |
| F-014 | Shuffle-replay determinism         | Cross-cut    | planned | F-008      | v0.11  | ~20   | [link](../planning/shuffle-replay-determinism/README.md) |

## Planned — SQL databases

| ID    | Title                              | Family   | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------|---------|------------|--------|-------|-------|
| F-015 | SQL schema & migration topology    | SQL      | planned | —          | v0.12  | ~84   | [link](../planning/sql-schema-and-migrations/README.md) |
| F-016 | SQL transaction & isolation matrix | SQL      | planned | F-015      | v0.12  | ~72   | [link](../planning/sql-transaction-isolation/README.md) |
| F-017 | SQL bulk + fast-restore            | SQL      | planned | F-010, F-011 | v0.13 | ~50   | [link](../planning/sql-bulk-and-fast-restore/README.md) |
| F-018 | SQL CDC / temporal / pubsub        | SQL      | planned | F-008, F-015 | v0.14 | ~54   | [link](../planning/sql-cdc-and-pubsub/README.md) |
| F-019 | SQL provider quirks (RLS, JSONB, FTS, columnstore) | SQL | planned | F-015 | v0.15 | ~60   | [link](../planning/sql-provider-quirks/README.md) |

## Planned — NoSQL databases

| ID    | Title                              | Family   | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------|---------|------------|--------|-------|-------|
| F-020 | NoSQL collection & index topology  | NoSQL    | planned | —          | v0.12  | ~98   | [link](../planning/nosql-collection-and-index-topology/README.md) |
| F-021 | NoSQL consistency & ETag conflicts | NoSQL    | planned | F-020      | v0.13  | ~62   | [link](../planning/nosql-consistency-and-conflicts/README.md) |
| F-022 | Change feed / change streams       | NoSQL    | planned | F-020      | v0.13  | ~62   | [link](../planning/nosql-change-feed-and-streams/README.md) |
| F-023 | Throughput / RU / cost assertions  | NoSQL    | planned | F-020      | v0.14  | ~34   | [link](../planning/nosql-throughput-and-cost/README.md) |
| F-024 | NoSQL provider quirks              | NoSQL    | planned | F-020      | v0.15  | ~80   | [link](../planning/nosql-provider-quirks/README.md) |

## Planned — Caching

| ID    | Title                              | Family   | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------|---------|------------|--------|-------|-------|
| F-025 | Cache stampede + tag invalidation + tier coherence | Caching | planned | F-008 | v0.13 | ~52 | [link](../planning/caching-stampede-and-tags/README.md) |
| F-026 | Distributed lock + serializer poisoning | Caching | planned | F-008      | v0.14  | ~42   | [link](../planning/caching-locks-and-poisoning/README.md) |

## Planned — Storage

| ID    | Title                              | Family   | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------|---------|------------|--------|-------|-------|
| F-027 | Storage bucket-lifecycle topology  | Storage  | planned | —          | v0.12  | ~70   | [link](../planning/storage-bucket-lifecycle-topology/README.md) |
| F-028 | Multipart / streaming / conditional writes | Storage | planned | F-027 | v0.13 | ~50  | [link](../planning/storage-multipart-and-conditional/README.md) |
| F-029 | SSE-KMS / object-lock / replication / lifecycle | Storage | planned | F-027 | v0.14 | ~60 | [link](../planning/storage-encryption-and-replication/README.md) |

## Planned — Security

| ID    | Title                              | Family    | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|-----------|---------|------------|--------|-------|-------|
| F-030 | JWT attacks + JWKS + key rotation  | Security  | planned | F-008      | v0.12  | ~28   | [link](../planning/jwt-attacks-and-jwks/README.md) |
| F-031 | mTLS revocation + chain + hostname | Security  | planned | F-008      | v0.13  | ~26   | [link](../planning/mtls-revocation-and-chain/README.md) |
| F-032 | OAuth flows + PKCE + DPoP + refresh rotation | Security | planned | F-008, F-030 | v0.14 | ~30 | [link](../planning/oauth-flows-and-pkce/README.md) |
| F-033 | Authz matrix + secrets/PII leak detection | Security | planned | F-030 | v0.15 | ~24 | [link](../planning/authz-matrix-and-leak-detection/README.md) |

## Planned — Observability

| ID    | Title                              | Family        | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|---------------|---------|------------|--------|-------|-------|
| F-034 | OTel cross-boundary propagation    | Observability | planned | F-012      | v0.12  | ~52   | [link](../planning/otel-cross-boundary-propagation/README.md) |
| F-035 | Log redaction + cardinality guard  | Observability | planned | —          | v0.13  | ~24   | [link](../planning/log-redaction-and-cardinality/README.md) |
| F-036 | Histogram / sampling assertions    | Observability | planned | F-034      | v0.14  | ~32   | [link](../planning/histogram-and-sampling-assertions/README.md) |
| F-037 | Async-context flow + Seq artefacts + AppInsights mock | Observability | planned | F-034 | v0.15 | ~42 | [link](../planning/async-context-and-seq-artefacts/README.md) |

## Planned — Microservices

| ID    | Title                              | Family         | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------------|---------|------------|--------|-------|-------|
| F-038 | Outbox / Inbox correctness         | Microservices  | planned | F-015      | v0.13  | ~62   | [link](../planning/outbox-inbox-correctness/README.md) |
| F-039 | Saga timeout & compensation        | Microservices  | planned | F-008, F-038 | v0.14 | ~26  | [link](../planning/saga-timeout-and-compensation/README.md) |
| F-040 | EventSourcing schema evolution + projection rebuild | Microservices | planned | F-038 | v0.15 | ~42 | [link](../planning/eventsourcing-evolution-and-projection/README.md) |
| F-041 | Consumer-driven contracts          | Microservices  | planned | —          | v0.15  | ~24   | [link](../planning/consumer-driven-contracts/README.md) |

## Planned — HTTP / gRPC

| ID    | Title                              | Family   | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|----------|---------|------------|--------|-------|-------|
| F-042 | HTTP streaming / SSE / WebSocket / HTTP2 | HTTP | planned | F-009      | v0.13  | ~28   | [link](../planning/http-streaming-and-protocols/README.md) |
| F-043 | HTTP cookies / redirects / CORS / negotiation | HTTP | planned | —      | v0.14  | ~24   | [link](../planning/http-cookies-redirects-cors/README.md) |
| F-044 | gRPC streaming / deadlines / metadata / retry | gRPC | planned | F-008  | v0.13  | ~28   | [link](../planning/grpc-streaming-and-deadlines/README.md) |
| F-045 | gRPC reconnection + compression + mTLS handler | gRPC | planned | F-031, F-044 | v0.14 | ~22 | [link](../planning/grpc-reconnection-and-mtls/README.md) |

## Planned — HealthChecks / Resilience / Concurrency / WebAPI / CI

| ID    | Title                              | Family       | Status  | Depends on | Target | Tasks | Brief |
|-------|------------------------------------|--------------|---------|------------|--------|-------|-------|
| F-046 | HealthCheck lifecycle (liveness / readiness / drain) | HealthChecks | planned | F-008 | v0.13 | ~26 | [link](../planning/healthcheck-lifecycle/README.md) |
| F-047 | Resilience composite policies + state machine | Resilience | planned | F-008, F-009 | v0.14 | ~28 | [link](../planning/resilience-composite-policies/README.md) |
| F-048 | Concurrency fuzzing + AsyncLocal flow | Concurrency | planned | F-014 | v0.15  | ~32   | [link](../planning/concurrency-fuzzing-and-asynccontext/README.md) |
| F-049 | WebAPI OpenAPI drift + ProblemDetails consistency | WebAPI | planned | — | v0.13   | ~24   | [link](../planning/webapi-openapi-drift/README.md) |
| F-050 | CI matrix pin + Docker resource simulation | CI / Docker | planned | — | v0.16    | ~26   | [link](../planning/ci-matrix-and-resource-simulation/README.md) |

---

## Summary

| Bucket | Features | Tasks |
|--------|---------:|------:|
| Cross-cutting (F-008–F-014) | 7  | ~437 |
| SQL family (F-015–F-019)    | 5  | ~320 |
| NoSQL family (F-020–F-024)  | 5  | ~336 |
| Caching (F-025–F-026)       | 2  | ~94  |
| Storage (F-027–F-029)       | 3  | ~180 |
| Security (F-030–F-033)      | 4  | ~108 |
| Observability (F-034–F-037) | 4  | ~150 |
| Microservices (F-038–F-041) | 4  | ~154 |
| HTTP / gRPC (F-042–F-045)   | 4  | ~102 |
| Health / Resilience / Concurrency / WebAPI / CI (F-046–F-050) | 5 | ~136 |
| **Total**                   | **43** | **~2 015** |

---

## How to pick up a planned feature

1. Read `planning/<slug>/README.md`. Confirm **Depends on** are shipped (or accept the dependency).
2. From repo root, on the default branch, run:
   ```bash
   /dai.spec  # paste the "Build prompt" section from the planning README when prompted
   ```
   This scaffolds `.dotnet-ai-kit/features/NNN-<slug>/spec.md` etc.
3. Update this file: flip the row's status to `specced` and add the SDD folder link.
4. Continue the SDD pipeline: `/dai.plan` → `/dai.tasks` → `/dai.implement` → `/dai.verify` → `/dai.pr`.
5. On merge to `master`: flip the row's status to `shipped` (or move it to the **Shipped** table at the top).

## Rules

- A planning brief never duplicates spec content — it's an **input** to `/dai.spec`, per [memory rule](../../.claude/rules/architecture-profile.md) `feedback_spec_home_is_sdd_feature_folder.md`.
- Numbering is monotonic. Cancelled features keep their ID and are marked `cancelled` in the row — IDs are never reused. See [ADR-009](adr/ADR-009-feature-numbering-after-007.md).
- Dependency edges in **Depends on** are advisory; an enforcement test (`RoadmapCompletenessTests`) is planned but not required for the planning-stage docs to land.
- This roadmap covers the gaps identified in the 2026-04-25 gap analysis; new gaps discovered later should append rows beyond F-050.
