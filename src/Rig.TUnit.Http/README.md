# Rig.TUnit.Http

In-memory HTTP mock with rich matcher matrix, response builders (status/headers/
JSON/binary/SSE), scenario state machine (per-call sequential responses), delay +
jitter + intermittent-failure injection, record/replay mode, and call verification.

## Install

```xml
<PackageReference Include="Rig.TUnit.Http" />
```

## Example

```csharp
var mock = new HttpMock();
mock.When.Post().Path("/orders")
    .JsonPathEquals("customer.id", "c-1")
    .Responds().WithStatus(HttpStatusCode.Created).WithJson("{\"id\":42}").And();

var client = mock.CreateClient();
var resp = await client.PostAsJsonAsync("http://mock/orders", new { customer = new { id = "c-1" } });

mock.Verify(HttpMethod.Post, "/orders").Called(1);
```

## Record / replay

```csharp
mock.RecordAgainst(new HttpClientHandler());
// ... run tests against real service ...
var recordings = mock.RecordedExchanges;

var replay = new HttpMock().ReplayFrom(recordings);
```

## Dependencies
- `Rig.TUnit.Core`

Spec: `003-rig-tunit-ecosystem-expansion` — US7.
