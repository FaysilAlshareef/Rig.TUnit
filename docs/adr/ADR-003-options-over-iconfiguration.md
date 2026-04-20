# ADR-003: IOptions<T> over IConfiguration in fixtures + helpers

**Status**: Accepted
**Date**: 2026-02 (Feature 002)
**Context**: Fixtures need configuration (image tag, credentials, timeouts). We
chose between `IConfiguration` direct access and strongly-typed `IOptions<T>`.

## Decision

Every fixture takes an `IOptions<{Provider}FixtureOptions>` (or a record constructor).
`IConfiguration` is **never** injected directly into fixtures or helpers.

## Rationale

1. **Validation** — `AddOptions<T>().ValidateDataAnnotations().ValidateOnStart()` fails
   fast at app start if config is wrong, instead of surfacing a `NullReferenceException`
   or wrong-type cast deep in a test.
2. **Discoverability** — every provider's `{Provider}FixtureOptions` class has a
   `public const string SectionName` that documents the expected config path
   (`"RigTUnit:Postgres"`, `"RigTUnit:Kafka"`, …). Enforced by
   `ProviderCompletenessTests`.
3. **Testability of defaults** — unit tests can construct `new FixtureOptions()` to
   verify defaults without a `ConfigurationBuilder` scaffold.
4. **CLAUDE rules compliance** — our `.claude/rules/configuration.md` requires
   `IOptions<T>` everywhere; fixtures are no exception.

## Consequences

- Every provider ships one Options class — documented in §6 of the canonical README
  template.
- Consumers bind options via:
  ```csharp
  services.AddOptions<PostgresFixtureOptions>()
      .BindConfiguration(PostgresFixtureOptions.SectionName)
      .ValidateDataAnnotations()
      .ValidateOnStart();
  ```
- CLI scripts that wanted raw `IConfiguration` access now go through a typed-options
  wrapper — one extra class per consumer, but a uniform shape.
