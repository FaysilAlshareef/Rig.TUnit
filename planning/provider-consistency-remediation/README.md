# Provider Consistency Remediation — Planning Folder

Feature **004-provider-consistency-remediation** — fills the uniformity gaps left by 003.

## What's here

| File | Purpose |
|---|---|
| [Rig.TUnit-Library-Design.md](Rig.TUnit-Library-Design.md) | Architectural design — problems, provider template, per-family gap list, .NET 10 driver strategy, phased delivery, acceptance criteria |
| [Rig.TUnit-Build-Prompt.md](Rig.TUnit-Build-Prompt.md) | `/dai.spec` input — paste verbatim to generate the formal spec |
| [Rig.TUnit-Session-Handoff.md](Rig.TUnit-Session-Handoff.md) | File-by-file checklist — version pins, every file to create, phased checkboxes, definition of done |
| [Rig.TUnit-Provider-Gap-Matrix.md](Rig.TUnit-Provider-Gap-Matrix.md) | Evidence snapshot — live gap matrix per family; update as work progresses |

## Why this feature exists

Feature 003 (ecosystem expansion) landed the base-package layer and most provider packages, but provider surface area is **inconsistent**:

- SqlServer, Sqlite, ServiceBus, Redis (cache) ship full `Fixture + Options + Builder + Extensions + Helpers`.
- Cassandra, Dynamo, ElasticSearch, EventStore, Nats, Sqs ship **only a fixture**.
- Mtls, Policies, Metrics ship **only a single class**.
- Five packages promised by 003 (`Cosmos`, `MySql`, `Oracle`, `AppInsights`, `Docker`) **were never created**.
- Test files mix tests with inline infrastructure.

This feature brings every provider to a uniform shape, ships the five missing packages, adds `SecurityRigBuilder<TSelf>`, and enforces the uniformity with architecture tests. No feature from 003 is deferred or deleted.

## Order of execution

1. Read `Rig.TUnit-Library-Design.md` end-to-end.
2. Paste `Rig.TUnit-Build-Prompt.md` into `/dai.spec`.
3. Drive implementation off `Rig.TUnit-Session-Handoff.md` (check the boxes).
4. Track progress by updating `Rig.TUnit-Provider-Gap-Matrix.md`.

## Branch

`feat/provider-consistency-remediation` off `master`.

## Related planning folders

- `planning/ecosystem-expansion/` — feature 003 baseline (defines the bases this feature wires providers into).
- `planning/base-library/` — historical (base library design; for format reference).
- `planning/fluent-builder-expansion/` — historical (002 feature; for format reference).
