# Rig.TUnit

A TUnit-first integration-testing rig ecosystem for .NET.

## Development Setup

After cloning, install the commit-message hook:

```bash
git config core.hooksPath .githooks
```

This enforces Conventional Commits prefixes (`test:`, `feat:`, `refactor:`, `fix:`, `chore:`, `docs:`, `style:`, `perf:`, `build:`, `ci:`, `revert:`) required by the TDD discipline. A GitHub Actions backstop (`.github/workflows/commit-msg-lint.yml`) lints PRs as a second line of defense.

## Repository Layout

- `src/` — library packages (base + provider pattern)
- `tests/` — unit, contract, and integration test projects
- `benchmarks/` — BenchmarkDotNet suites and historical baselines
- `.dotnet-ai-kit/features/` — spec-driven development artifacts
- `.githooks/` — git hooks (commit-msg enforces TDD cadence)
