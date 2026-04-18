# Rig.TUnit.Security.Policies

Policy-based authorization testing for Rig.TUnit. Registers a real
`IAuthorizationService` in an in-process DI container so your production
`AuthorizationHandler<T>` implementations execute unchanged — no test bypass.

## Install

```bash
dotnet add package Rig.TUnit.Security.Policies
```

## Quick start

```csharp
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Rig.TUnit.Security.Policies;
using Rig.TUnit.Security.Policies.Fixtures;

await using var fx = new PolicyFixture(
    new PolicyFixtureOptions(),
    policies => policies.AddPolicy("AdminOnly", p => p.RequireRole("admin")));
await fx.InitializeAsync();

var admin = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "admin") }, "Test"));
await PolicyAssert.Policy(fx.Services, "AdminOnly").Allows(admin);
```

## Fluent wiring

```csharp
services.AddRigTUnit(rig =>
    rig.UsePolicies(RigConnect.FromValue("Test"), cfg => { })
);
```

## Options

| Property | Default | Purpose |
|---|---|---|
| `DefaultScheme` | `Test` | Authentication scheme name surfaced via `ConnectionString` |
| `RequiredClaims` | `[]` | Claims expected on every principal under test |

Use `PolicyAssert.Policy(services, name).Allows(...)` / `.Denies(...)` to verify
policy decisions end-to-end.
