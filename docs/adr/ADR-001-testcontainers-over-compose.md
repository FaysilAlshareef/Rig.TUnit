# ADR-001: Testcontainers over docker-compose for integration-test infrastructure

**Status**: Accepted
**Date**: 2026-01 (Feature 001)
**Context**: Rig.TUnit must spin up broker/DB/cache containers per integration test run.

## Decision

Every container-backed fixture uses **Testcontainers for .NET** rather than a
`docker-compose.yml` file checked into the test project.

## Rationale

1. **Per-test lifecycle** — Testcontainers cleans up containers when the test host
   exits, even on crash. `docker-compose up` leaks containers on cancellation.
2. **Port + volume isolation** — Testcontainers assigns ephemeral ports and temp
   volumes by default, so parallel test runs never collide. Compose requires manual
   port mapping and per-runner unique project names.
3. **Programmatic configuration** — fixtures can pass `IOptions<TOptions>` directly
   into the builder (image tag, password, startup timeout) without templating YAML.
4. **CI parity** — same C# code runs locally and on GitHub Actions; no compose-file
   divergence to maintain.

## Consequences

- Consumers need Docker available at test time (documented in every leaf provider's
  §4 Prerequisites).
- Tests that wrap multi-container systems (Kafka + Zookeeper, ES + Kibana) still use
  Testcontainers Networks — not compose.
- Developers wanting a manual local stack still run `docker-compose` for exploratory
  work; the test infrastructure doesn't depend on it.
