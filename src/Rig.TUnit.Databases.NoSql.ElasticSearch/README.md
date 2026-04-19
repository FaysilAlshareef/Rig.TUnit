# Rig.TUnit.Databases.NoSql.ElasticSearch

Testcontainers-backed Elasticsearch 8.x provider. Ships `ElasticSearchFixture` (self-signed-cert-trusting `ElasticsearchClient` with HTTPS + basic auth), `ElasticSearchFixtureOptions`, `ElasticSearchRigBuilder`, the `UseElasticSearch(source, cfg => ...)` fluent entry, plus `IndexRefreshHelper` (forces near-real-time index refresh) and `DslAssert.HitCountAsync<T>` (strongly-typed hit-count fluent assertion).

## Install

```
dotnet add package Rig.TUnit.Databases.NoSql.ElasticSearch
```

## Example

```csharp
public sealed class OrderSearchTests
{
    private readonly ElasticSearchFixture _es = new();

    [Before(Test)] public Task Init() => _es.InitializeAsync();
    [After(Test)]  public ValueTask Disp() => _es.DisposeAsync();

    [Test]
    public async Task IndexedDocs_Are_Findable()
    {
        var index = "orders";
        await _es.Client.Indices.CreateAsync(index);
        await _es.Client.IndexAsync(new { Sku = "X-1", Qty = 2 }, i => i.Index(index));
        await IndexRefreshHelper.RefreshAsync(_es.Client, index);

        var hits = await DslAssert.HitCountAsync<object>(
            _es.Client, index, s => s.Query(q => q.MatchAll(_ => { })));
        await Assert.That(hits).IsEqualTo(1L);
    }
}
```

## Dependencies

`Rig.TUnit.Databases.NoSql`, `Testcontainers.Elasticsearch`, `Elastic.Clients.Elasticsearch`
