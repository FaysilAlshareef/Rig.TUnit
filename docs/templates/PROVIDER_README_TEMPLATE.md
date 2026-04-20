<!-- Rig.TUnit canonical provider README template — 14 sections — Feature 005 T123 -->
<!-- SECTION 1 -->
# Rig.TUnit.{Family}.{Provider}

> One-sentence elevator pitch. What is this provider for?

<!-- SECTION 2 -->
## What this package is

1–2 paragraphs explaining the _concept_ — not the API. Answer: what test scenarios does this make possible, and what does the library handle so the test author doesn't have to?

<!-- SECTION 3 -->
## When to use it

Bullet list of 3–5 concrete scenarios where this is the right tool. Explicit "not for" anti-scenarios in sub-bullets.

<!-- SECTION 4 -->
## Prerequisites

- .NET version
- External dependencies (Docker image, cloud emulator, SDK)
- Any IAM / credential bootstrap

<!-- SECTION 5 -->
## Quick start

```csharp
// Runnable snippet — MUST compile. Feature 005 T161/T162's snippet-extraction
// CI job extracts this block and builds it.
```

<!-- SECTION 6 -->
## Options

Reflect every `public` property on `{Provider}FixtureOptions` as a table:

| Property | Type | Default | Description |
|---|---|---|---|
| `ImageTag` | `string` | `"latest"` | Container image tag |
| … | … | … | … |

<!-- SECTION 7 -->
## Fixture + helper APIs

Bullet list of the core types: `{Provider}Fixture`, `{Provider}RigBuilder`, and any helper types (`{Provider}PerTestHelper`, `{Provider}Listener`, `{Provider}EventSender`, etc.).

<!-- SECTION 8 -->
## Per-test isolation

Explain how this provider isolates tests from each other. Three canonical strategies: (1) ephemeral database/schema/collection/keyspace; (2) IsolationKey-prefixed names; (3) explicit reset between tests. State which one this provider uses and why.

<!-- SECTION 9 -->
## Parallelism + performance

Expected startup time, steady-state per-test cost, and parallelism caveats (e.g., Kafka broker bind-port contention, Cosmos emulator's Linux-only constraint).

<!-- SECTION 10 -->
## Troubleshooting

Common failure modes with observed symptoms and fixes. Link to `docs/troubleshooting.md#{provider}` for the consolidated catalogue.

<!-- SECTION 11 -->
## Provider quirks + edge cases

Things that surprise first-time users: MySQL's non-case-sensitive identifiers, Postgres's serial-vs-identity, Oracle's `CREATE USER` session overhead, DynamoDB-Local's eventually-consistent reads, etc.

<!-- SECTION 12 -->
## Benchmarks

Link to `tests/Rig.TUnit.Benchmarks/{Provider}*Benchmarks.cs` and the latest baseline-005.json entries. Callouts for any known hot paths.

<!-- SECTION 13 -->
## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- [Troubleshooting](../../docs/troubleshooting.md)
- [Performance tuning](../../docs/performance-tuning.md)
- Family base package: `Rig.TUnit.{Family}`

<!-- SECTION 14 -->
## License

MIT. See [LICENSE](../../LICENSE).
