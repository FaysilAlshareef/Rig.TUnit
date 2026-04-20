# Rig.TUnit Documentation Quality Bar

Reviewer rubric for provider READMEs + supporting docs. Every section of a
provider README is scored on a 3-level scale.

## Per-section rubric

| Grade | Meaning | Reviewer action |
|---|---|---|
| **Pass** | Content answers the section's question completely; concrete and provider-specific | Merge |
| **Needs work** | Present but generic / vague / missing at least one load-bearing fact | Request specific additions before merge |
| **Missing** | Section absent, or consists solely of a "TODO" / placeholder | Block merge until resolved |

## Section-specific grading criteria

### §1 — Title
- **Pass**: includes the canonical package name + one-sentence pitch
- **Needs work**: pitch present but generic ("testing library for X")
- **Missing**: no pitch

### §2 — What this package is
- **Pass**: explains test scenarios enabled + what the library handles so the test author doesn't
- **Needs work**: API-focused rather than scenario-focused
- **Missing**: copies the product tagline

### §3 — When to use it
- **Pass**: 3+ concrete scenarios + ≥1 anti-scenario
- **Needs work**: scenarios present but vague ("integration tests")
- **Missing**: no scenarios listed

### §4 — Prerequisites
- **Pass**: .NET version + external dependencies + credential bootstrap
- **Needs work**: some prerequisites listed but incomplete
- **Missing**: section absent

### §5 — Quick start
- **Pass**: runnable C# snippet that compiles AND exercises the fixture end-to-end
- **Needs work**: snippet present but stubbed (`// TODO`, `throw`) or doesn't compile
- **Missing**: no snippet

### §6 — Options
- **Pass**: every public property on `{Provider}FixtureOptions` is tabled with type + default + description
- **Needs work**: partial table (missing properties or defaults)
- **Missing**: no table

### §7 — Fixture + helper APIs
- **Pass**: every public type is bulleted with a one-line purpose
- **Needs work**: types listed but purpose unclear
- **Missing**: no API list

### §8 — Per-test isolation
- **Pass**: explicit strategy (ephemeral DB / IsolationKey-prefix / reset-between-tests) with rationale
- **Needs work**: mentions isolation but strategy unclear
- **Missing**: isolation not addressed

### §9 — Parallelism + performance
- **Pass**: concrete numbers (startup ms, per-test overhead) + parallelism caveats
- **Needs work**: caveats mentioned but no numbers
- **Missing**: section absent (valid for in-process providers — use `## §9 — N/A: in-process` placeholder)

### §10 — Troubleshooting
- **Pass**: ≥3 known failure modes with symptoms + fixes
- **Needs work**: ≥1 mode documented
- **Missing**: section absent

### §11 — Provider quirks + edge cases
- **Pass**: ≥1 non-obvious behaviour called out with rationale
- **Needs work**: quirks mentioned but not explained
- **Missing**: section absent (valid for uniform providers — use `## §11 — N/A` placeholder)

### §12 — Benchmarks
- **Pass**: link to `tests/Rig.TUnit.Benchmarks/{Provider}*Benchmarks.cs` + baseline-005 entry reference
- **Needs work**: link present but no baseline mention
- **Missing**: section absent

### §13 — Related docs
- **Pass**: links to architecture diagram + glossary + troubleshooting + performance tuning + family base
- **Needs work**: some links present
- **Missing**: no cross-links

### §14 — License
- **Pass**: "MIT. See [LICENSE](…)."
- **Needs work**: license named but no link
- **Missing**: no license declaration

## N/A placeholders

Meta-packages (`Rig.TUnit.All`, `Rig.TUnit`, family-base packages) may legitimately skip
§9/§10/§12 with a `## §N — N/A: <rationale>` placeholder. The Markdig-based
`ReadmeCompletenessTests` (FR-066) accepts `N/A` placeholders as present.

## Reviewer decision gate

A README passes overall review if:
- 0 sections graded **Missing**
- ≤ 2 sections graded **Needs work**
- §5 (Quick start) graded **Pass** — snippet must compile

Failing any of these blocks the merge until addressed.
