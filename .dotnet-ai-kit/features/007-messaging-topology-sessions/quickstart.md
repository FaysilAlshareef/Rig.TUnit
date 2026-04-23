# Quickstart — 007-messaging-topology-sessions

**Generated**: 2026-04-23
**Audience**: implementer picking up a task from the plan; reviewer validating a PR locally.

This quickstart covers what's needed to **run, verify, and debug** Feature 007 work on a developer workstation. It does not reproduce the spec — use [spec.md](spec.md) for requirements, [plan.md](plan.md) for sequencing.

---

## Prerequisites

| Tool | Version | Purpose |
|------|---------|---------|
| .NET SDK | **10.0.100** (per `global.json`) | Build / test |
| Docker | ≥ 25.x | Container fixtures (every provider) |
| Docker Compose v2 | built into Docker | ServiceBus emulator, RabbitMQ, NATS, LocalStack |
| Kafka image | (pulled automatically by fixture) | Kafka integration |
| PowerShell or Bash | — | Either shell works; examples below use Bash (repo convention for Claude) |
| `gh` | ≥ 2.40 | PR creation |

**Azure Service Bus emulator**: image `mcr.microsoft.com/azure-messaging/servicebus-emulator:1.1.2` or newer. Pulled via the existing `docker-compose.yml` at [`tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/`](../../../tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/).

**NATS JetStream** (new, Phase 5 only): same `nats:2.10-alpine` image as core NATS — JetStream is enabled with the `-js` flag on startup.

---

## Branch setup (one-time per developer)

```bash
git fetch origin
git checkout -b feat/007-messaging-topology-sessions origin/master
```

Feature 006 exit gates must be green on `master` before this branch is cut. Verify:

```bash
git log origin/master --oneline | head -5
# Expect to see: "Merge pull request #7 from FaysilAlshareef/feat/006-coverage-quality-uplift"
```

---

## Build the whole solution

```bash
dotnet restore
dotnet build Rig.TUnit.slnx
```

Expected: clean build with no new warnings. Any new XML-doc warning on a new public type is a reviewer-block.

---

## Per-phase verification loop

### Phase 0 — base library

```bash
# RED: add the test, confirm it fails, commit.
dotnet test tests/Rig.TUnit.Messaging.Tests.Unit/Rig.TUnit.Messaging.Tests.Unit.csproj \
  --filter "FullyQualifiedName~SendContextTests"
# Expect: failure (SendContext doesn't exist yet).

git add tests/Rig.TUnit.Messaging.Tests.Unit/Helpers/SendContextTests.cs
git commit -m "test(007): RED T000 — SendContext record shape"

# GREEN: add production, confirm test passes, commit.
# (… edit src/Rig.TUnit.Messaging/Helpers/SendContext.cs + EventSenderBase.cs …)
dotnet test tests/Rig.TUnit.Messaging.Tests.Unit/Rig.TUnit.Messaging.Tests.Unit.csproj \
  --filter "FullyQualifiedName~SendContextTests"
# Expect: all green.

git add src/Rig.TUnit.Messaging/Helpers/SendContext.cs \
        src/Rig.TUnit.Messaging/Helpers/EventSenderBase.cs \
        src/Rig.TUnit.Messaging/Helpers/ListenerBase.cs \
        README.md
git commit -m "feat(007): GREEN T000 — SendContext + BuildHeaders overload"
```

Same pattern for T001, T002, T003. Final Phase-0 step is landing an **empty** `.parity-coverage.txt`:

```bash
touch tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt
git add tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt \
        tests/Rig.TUnit.Architecture.Tests/Rules/ProviderCompletenessTests.cs
git commit -m "feat(007): GREEN T003 — progressive parity enforcement driver"
```

### Phase 1 — Azure Service Bus

Bring up the emulator:

```bash
docker compose -f tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/TestInfrastructure/docker-compose.yml up -d
```

Run the capability probe **first** (T014 — answers risk R1):

```bash
dotnet test tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/Rig.TUnit.Messaging.ServiceBus.Tests.Integration.csproj \
  --filter "FullyQualifiedName~ServiceBusEmulatorCapabilityProbeTests"
```

Inspect any `Assert.Inconclusive` result — if the probe flags an op Phase 1 depends on, apply C-004 (annotate the affected test `[Skip("emulator-gap: …")]` + add a row to `docs/providers/service-bus.md#emulator-gaps`).

Then follow the phase's RED/GREEN sequence as laid out in [plan.md §Phase 1](plan.md). After T013 GREEN, confirm `.parity-coverage.txt` now contains `Rig.TUnit.Messaging.ServiceBus`:

```bash
cat tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt
# Rig.TUnit.Messaging.ServiceBus
```

### Phase 2 — Kafka

```bash
# Shared Kafka fixture starts the container automatically on first test run.
dotnet test tests/Rig.TUnit.Messaging.Kafka.Tests.Integration/ \
  --filter "FullyQualifiedName~Partitions"
```

After T023 GREEN, verify the parity file:

```bash
cat tests/Rig.TUnit.Architecture.Tests/.parity-coverage.txt
# Rig.TUnit.Messaging.ServiceBus
# Rig.TUnit.Messaging.Kafka
```

### Phase 3 — SQS FIFO

```bash
# LocalStack container starts via SharedSqsFixture.
dotnet test tests/Rig.TUnit.Messaging.Sqs.Tests.Integration/ --filter "FullyQualifiedName~Fifo"
```

**Dedup-window trap** (R4): if re-running the same test within 5 minutes and it fails with "message not delivered", confirm `DeduplicationKey` includes `IsolationKey` prefix. Any test hard-coding a constant `DeduplicationKey` is the bug.

### Phase 4 — RabbitMQ

```bash
dotnet test tests/Rig.TUnit.Messaging.RabbitMq.Tests.Integration/ --filter "FullyQualifiedName~Topology"
```

### Phase 5 — NATS JetStream

```bash
# JetStream container is a separate service — start once.
docker compose -f tests/Rig.TUnit.Messaging.Nats.Tests.Integration/docker-compose.yml up -d jetstream

dotnet test tests/Rig.TUnit.Messaging.Nats.Tests.Integration/ --filter "FullyQualifiedName~JetStream"
```

---

## Running the full architecture tests

Always runs locally — confirms parity progression:

```bash
dotnet test tests/Rig.TUnit.Architecture.Tests/ --filter "FullyQualifiedName~ProviderCompleteness"
```

If this fails, either (a) you added a provider to `.parity-coverage.txt` before landing its `WithTopology` / `SendContext` / session-listener surface, or (b) you removed a parity surface that `.parity-coverage.txt` still claims.

---

## Running the compile-fence tests

Per C-003, each provider ships a unit test asserting its topology-builder interface does **not** expose methods for concepts the broker can't model. Locally:

```bash
dotnet test tests/Rig.TUnit.Messaging.Kafka.Tests.Unit/ \
  --filter "FullyQualifiedName~CompileFence"
# All 5 provider test projects have an equivalent CompileFence test class.
```

If a developer added `.Queue(...)` to `IKafkaTopologyBuilder` "just to be consistent", the CompileFence test catches it before merge.

---

## Coverage verification

```bash
# Per-package coverage run.
dotnet test Rig.TUnit.slnx \
  --collect:"XPlat Code Coverage" \
  --results-directory ./TestResults \
  --settings .runsettings
```

Then:

```bash
# Regenerate summary.csv locally to cross-check against the gate.
reportgenerator \
  -reports:./TestResults/**/coverage.cobertura.xml \
  -targetdir:./coverage-report \
  -reporttypes:"CsvSummary;HtmlSummary"

# Inspect packages touched by Feature 007.
grep -E "Rig\.TUnit\.Messaging(\.ServiceBus|\.Kafka|\.Sqs|\.RabbitMq|\.Nats)?," \
     ./coverage-report/Summary.csv
```

Reviewer rule: **every new public type** (see [data-model.md](data-model.md)) at 100 % line.

---

## Benchmarks (Phase 6)

```bash
# ServiceBus session-vs-non-session throughput benchmark.
dotnet run -c Release --project tests/Rig.TUnit.Benchmarks/ -- \
  --filter "*ServiceBus*Session*"

# Kafka multi-partition per-key throughput.
dotnet run -c Release --project tests/Rig.TUnit.Benchmarks/ -- \
  --filter "*Kafka*Multi*"
```

Baseline goes to `benchmarks/baseline-007.json` (or `baseline-006.json` extended — either works per Q-4 resolution).

---

## PR checklist (for reviewer)

Before approving any Feature 007 PR:

- [ ] Branch is `feat/007-messaging-topology-sessions`.
- [ ] No `--no-verify`, no `--no-gpg-sign` in any commit.
- [ ] RED commits precede GREEN commits per task; no amend across the boundary.
- [ ] Every integration-scenario task (T015a–d, T025a–b, T033a–c, T044a–d, T055a–c) has a discrete RED+GREEN pair.
- [ ] Every new public type is 100 % line-covered (Codecov diff).
- [ ] Inline XML docs on every new public type and parameter.
- [ ] Affected `docs/providers/*.md` updated in the same PR.
- [ ] `.parity-coverage.txt` diff matches the declared provider scope of the PR.
- [ ] Compile-fence unit test present for any provider-specific interface introduced.
- [ ] If an emulator gap was hit: the skipped test has `[Skip(" …")]`, `docs/providers/service-bus.md#emulator-gaps` has a row, and the upstream issue link is in the PR body.
- [ ] `Rig.TUnit.Architecture.Tests` run locally — green.

---

## Troubleshooting

| Symptom | Likely cause | Fix |
|---------|--------------|-----|
| `ProviderCompletenessTests` fails with "no WithTopology method" | Provider phase's GREEN T0X3 hasn't declared the hook yet, but the assembly was added to `.parity-coverage.txt` prematurely. | Remove the line from `.parity-coverage.txt` until the hook lands. |
| `[Skip("emulator-gap: …")]` annotations multiplying | Emulator version mismatch. | Pin the emulator image tag in `docker-compose.yml` to a known-good version; re-run `ServiceBusEmulatorCapabilityProbeTests`. |
| SQS test fails on 2nd run within 5 min | FIFO dedup window still holds the previous `DeduplicationKey`. | Confirm test prefixes with `IsolationKey`; if it does, restart LocalStack container. |
| Kafka test hangs on "waiting for partitions assigned" | Multi-partition rebalance did not complete. | Consumer's `partitionsAssigned` TCS pattern from [`KafkaListener.cs:66`](../../../src/Rig.TUnit.Messaging.Kafka/Helpers/KafkaListener.cs:66) is the canonical fix — reuse it. |
| JetStream consumer duplicates messages across reconnect | Ordered consumer config missing `FlowControl=true`. | Check `NatsJetStreamListener` config literal against [data-model.md §Nats](data-model.md). |
| `dotnet format --verify-no-changes` fails on new files | File-scoped namespace, trailing newline, or `var` / `sealed` conventions missed. | Run `dotnet format` before committing. |

---

## Command reference

Quick copy-paste bundle:

```bash
# Restore + build
dotnet restore && dotnet build Rig.TUnit.slnx

# Per-package test (fast)
dotnet test tests/Rig.TUnit.Messaging.Tests.Unit/
dotnet test tests/Rig.TUnit.Messaging.ServiceBus.Tests.Unit/
dotnet test tests/Rig.TUnit.Messaging.ServiceBus.Tests.Integration/

# Full architecture parity run
dotnet test tests/Rig.TUnit.Architecture.Tests/

# Format check
dotnet format --verify-no-changes

# Coverage collect (full)
dotnet test Rig.TUnit.slnx --collect:"XPlat Code Coverage"
```

---

## Next

Once Phase 0 is green and merged:

- `/dotnet-ai-kit:tasks` — break phases into explicit, ordered executable items with file paths and commit prefixes.
- `/dotnet-ai-kit:implement` — execute tasks phase by phase.
- `/dotnet-ai-kit:verify` — run build / test / format / coverage / parity gates before PR.
