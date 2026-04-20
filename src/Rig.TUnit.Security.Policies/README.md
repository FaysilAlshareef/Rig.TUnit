# Rig.TUnit.Security.Policies

> Policy-based authorisation testing — real `IAuthorizationService` in-process so production `AuthorizationHandler<T>` runs unchanged.

## What this package is

Runs your ASP.NET Core `AuthorizationHandler<TRequirement>`
implementations inside a real `IAuthorizationService` — no test bypass,
no `[AllowAnonymous]` shortcut. `PolicyFixture` constructs the DI
container with policies you declare, and `PolicyAssert.Policy(…).Allows
(principal)` / `.Denies(principal)` executes the full pipeline and
asserts the decision.

## When to use it

- Unit-testing policy definitions — does "AdminOnly" actually require
  the role you think it does?
- Verifying custom `AuthorizationHandler<T>` behaviour end-to-end.
- Regression-testing policy changes after a claims-transformation refactor.
- **Not for**: full HTTP integration — layer with `Rig.TUnit.WebAPI` and
  send real requests.

## Prerequisites

- .NET 10 SDK
- Project under test uses ASP.NET Core authorisation (package reference:
  `Microsoft.AspNetCore.Authorization`).

## Quick start

```csharp
using System.Security.Claims;
using Rig.TUnit.Security.Policies.Fixtures;

await using var fx = new PolicyFixture(
    new PolicyFixtureOptions(),
    policies => policies.AddPolicy("AdminOnly", p => p.RequireRole("admin")));
await fx.InitializeAsync();
```

## Options

| Property | Type | Default | Description |
|---|---|---|---|
| `DefaultScheme` | `string` | `"Test"` | Scheme name surfaced on the fixture's ConnectionString |
| `RequiredClaims` | `string[]` | `[]` | Claims auto-asserted on every principal |
| `LogDecisions` | `bool` | `false` | Emit structured log per allow/deny |

## Fixture + helper APIs

- `Rig.TUnit.Security.Policies.Fixtures.PolicyFixture`
- `Rig.TUnit.Security.Policies.Options.PolicyFixtureOptions`
- `Rig.TUnit.Security.Policies.Builder.PoliciesRigBuilder`
- `Rig.TUnit.Security.Policies.Assertions.PolicyAssert`

## Per-test isolation

`PolicyFixture` owns its DI container per-test; policies registered in
one fixture do not leak into another. Safe under full parallelism.

## Parallelism + performance

- Fixture construction: ~5 ms (DI container + policy registry).
- Per-assertion: ~100 µs (`IAuthorizationService.AuthorizeAsync`).
- Safe under full parallelism.

## Troubleshooting

- **`AuthorizationResult.Failed` on an expected-allow** — confirm the
  `ClaimsPrincipal` has a non-null `IsAuthenticated` identity; anonymous
  principals fail most policies.
- **Handler never invoked** — ensure `RequireAssertion` or
  `RequireRole` is on the policy; a policy without any requirement
  succeeds by default.

See [docs/troubleshooting.md#policies](../../docs/troubleshooting.md).

## Provider quirks + edge cases

- `AuthorizationOptions.DefaultPolicy` is respected — tests without an
  explicit policy fall through to it.
- Multiple `AuthorizationHandler<T>` for the same requirement all run;
  any `context.Succeed(…)` grants — tests asserting "no handler ran"
  must track this carefully.

## Benchmarks

See [`PoliciesBenchmarks.cs`](../../tests/Rig.TUnit.Benchmarks/PoliciesBenchmarks.cs);
baseline in `benchmarks/baseline-005.json`.

## Related docs

- [Architecture diagram](../../docs/architecture-diagram.md)
- [Glossary](../../docs/glossary.md)
- Family base: [`Rig.TUnit.Security`](../Rig.TUnit.Security/README.md)

## License

MIT. See [LICENSE](../../LICENSE).
