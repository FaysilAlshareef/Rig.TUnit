using System.Reflection;
using Rig.TUnit.Architecture.Tests.Infrastructure;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// FR-001: every <c>Rig.TUnit.{Family}.{Provider}</c> package must expose the canonical
/// quartet — <c>{Provider}Fixture</c>, <c>{Provider}FixtureOptions</c> (with
/// <c>public const string SectionName</c>), <c>{Provider}RigBuilder</c>, and a public static
/// <c>Use{Provider}</c> extension on <c>RigBuilder</c>.
///
/// Phase 1 lands the rule RED-visible: providers that currently lack any of the four live in
/// <see cref="SkipUntilFixed"/> with the task ID that closes the gap. As each is completed in
/// Phase 3/4, move it from <c>SkipUntilFixed</c> into <see cref="RequiredProviders"/>.
///
/// Deliberately does NOT duplicate <see cref="CodeOrganizationTests.AllFixtures_ExtendFixtureBase"/>
/// (analysis finding #5): this rule only enforces the presence of the four canonical types.
/// It reuses <see cref="AssemblyLoader"/> so Phase-4 packages appear automatically when they land.
/// </summary>
public sealed class ProviderCompletenessTests
{
    private sealed record ProviderEntry(
        string AssemblyName,
        string FixtureName,
        string OptionsName,
        string BuilderName,
        string UseMethodName);

    /// <summary>
    /// Providers that MUST pass every check today. Phase 3/4 grows this list.
    /// Caching.Redis ships under the <c>RedisCache*</c> naming convention to disambiguate the
    /// cache role from <c>Rig.TUnit.Databases.NoSql.Redis</c>'s KV role — names encoded here.
    /// </summary>
    private static readonly ProviderEntry[] RequiredProviders =
    [
        new("Rig.TUnit.Databases.Sql.SqlServer",  "SqlServerFixture",  "SqlServerFixtureOptions",  "SqlServerRigBuilder",  "UseSqlServer"),
        new("Rig.TUnit.Databases.Sql.Sqlite",     "SqliteFixture",     "SqliteFixtureOptions",     "SqliteRigBuilder",     "UseSqlite"),
        new("Rig.TUnit.Databases.Sql.Postgresql", "PostgresFixture",   "PostgresFixtureOptions",   "PostgresRigBuilder",   "UsePostgres"),
        new("Rig.TUnit.Databases.NoSql.Mongo",    "MongoFixture",      "MongoFixtureOptions",      "MongoRigBuilder",      "UseMongo"),
        new("Rig.TUnit.Databases.NoSql.Cassandra","CassandraFixture",  "CassandraFixtureOptions",  "CassandraRigBuilder",  "UseCassandra"),
        new("Rig.TUnit.Databases.NoSql.Dynamo",   "DynamoFixture",     "DynamoFixtureOptions",     "DynamoRigBuilder",     "UseDynamo"),
        new("Rig.TUnit.Databases.NoSql.ElasticSearch","ElasticSearchFixture","ElasticSearchFixtureOptions","ElasticSearchRigBuilder","UseElasticSearch"),
        new("Rig.TUnit.Databases.NoSql.KurrentDb","KurrentDbFixture",  "KurrentDbFixtureOptions",  "KurrentDbRigBuilder",  "UseKurrentDb"),
        new("Rig.TUnit.Messaging.ServiceBus",     "ServiceBusFixture", "ServiceBusFixtureOptions", "ServiceBusRigBuilder", "UseServiceBus"),
        new("Rig.TUnit.Messaging.Kafka",          "KafkaFixture",      "KafkaFixtureOptions",      "KafkaRigBuilder",      "UseKafka"),
        new("Rig.TUnit.Messaging.RabbitMq",       "RabbitMqFixture",   "RabbitMqFixtureOptions",   "RabbitMqRigBuilder",   "UseRabbitMq"),
        new("Rig.TUnit.Messaging.Nats",           "NatsFixture",       "NatsFixtureOptions",       "NatsRigBuilder",       "UseNats"),
        new("Rig.TUnit.Messaging.Sqs",            "SqsFixture",        "SqsFixtureOptions",        "SqsRigBuilder",        "UseSqs"),
        new("Rig.TUnit.Caching.Redis",            "RedisFixture",      "RedisFixtureOptions",      "RedisCacheRigBuilder", "UseRedisCache"),
        new("Rig.TUnit.Caching.Hybrid",           "HybridCacheFixture","HybridCacheFixtureOptions","HybridCacheRigBuilder","UseHybridCache"),
        new("Rig.TUnit.Caching.Fusion",           "FusionCacheFixture","FusionCacheFixtureOptions","FusionCacheRigBuilder","UseFusionCache"),
        new("Rig.TUnit.Storage.AzureBlob",        "AzureBlobFixture", "AzureBlobFixtureOptions", "AzureBlobRigBuilder",  "UseAzureBlob"),
        new("Rig.TUnit.Storage.S3",               "S3Fixture",        "S3FixtureOptions",        "S3RigBuilder",         "UseS3"),
        new("Rig.TUnit.Storage.MinIO",            "MinIOFixture",     "MinIOFixtureOptions",     "MinIORigBuilder",      "UseMinIO"),
        new("Rig.TUnit.Storage.FileSystem",       "FileSystemFixture","FileSystemFixtureOptions","FileSystemRigBuilder", "UseFileSystem"),
    ];

    /// <summary>
    /// Providers deliberately skipped until their phase closes the gap. Each entry names the
    /// closing task (or "by-design" for telemetry-style providers that ship a different shape).
    /// Remove an entry and add the matching <see cref="ProviderEntry"/> to
    /// <see cref="RequiredProviders"/> when a provider reaches canonical shape.
    /// </summary>
    private static readonly (string Assembly, string ClosingTask)[] SkipUntilFixed =
    [
        ("Rig.TUnit.Caching.Memory",                  "by-design — in-process cache, no FixtureOptions/container (T056/T057 confirmed no gap)"),
        ("Rig.TUnit.Security.Jwt",                    "T079-T080"),
        ("Rig.TUnit.Security.OAuth",                  "T081-T082"),
        ("Rig.TUnit.Security.Mtls",                   "T083-T086"),
        ("Rig.TUnit.Security.Policies",               "T087-T090"),
        ("Rig.TUnit.Observability.Metrics",           "T091-T095"),
        ("Rig.TUnit.Observability.Logging",           "by-design — telemetry-style (no fluent Use extension)"),
        ("Rig.TUnit.Observability.Tracing",           "by-design — telemetry-style"),
        ("Rig.TUnit.Observability.Seq",               "by-design — telemetry-style"),
        ("Rig.TUnit.Databases.Sql.MySql",             "T100-T107 (Phase 4)"),
        ("Rig.TUnit.Databases.Sql.Oracle",            "T108-T115 (Phase 4)"),
        ("Rig.TUnit.Databases.NoSql.Cosmos",          "T116-T123 (Phase 4)"),
        ("Rig.TUnit.Observability.AppInsights",       "T124-T131 (Phase 4)"),
        ("Rig.TUnit.Docker",                          "T132-T137 (Phase 4 — ships DockerFixtureOptions/Builder/Extensions)"),
    ];

    [Test]
    public async Task RequiredProviders_ExposeCanonicalTypes()
    {
        var offenders = new List<string>();

        foreach (var p in RequiredProviders)
        {
            var assembly = AssemblyLoader.TryLoad(p.AssemblyName);
            if (assembly is null)
            {
                // Phase-by-phase rollout: assemblies not transitively referenced by
                // Rig.TUnit.Architecture.Tests.csproj won't be in bin. The rule is a contract
                // on types — skip when the assembly is absent, matching existing rule pattern
                // (CoverageRuleTests, DependencyDirectionTests). Wire the Architecture.Tests
                // project to reference Rig.TUnit.All to activate every provider check at once.
                continue;
            }

            var types = assembly.GetExportedTypes();

            if (!types.Any(t => t is { IsClass: true, IsAbstract: false } && t.Name == p.FixtureName))
            {
                offenders.Add($"{p.AssemblyName}: missing concrete class {p.FixtureName}");
            }

            var optionsType = types.FirstOrDefault(t => t.IsClass && t.Name == p.OptionsName);
            if (optionsType is null)
            {
                offenders.Add($"{p.AssemblyName}: missing class {p.OptionsName}");
            }
            else
            {
                var sectionName = optionsType.GetField(
                    "SectionName",
                    BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy);

                if (sectionName is null || !sectionName.IsLiteral || sectionName.FieldType != typeof(string))
                {
                    offenders.Add($"{p.AssemblyName}.{p.OptionsName}: missing `public const string SectionName`");
                }
            }

            if (!types.Any(t => t.IsClass && t.Name == p.BuilderName))
            {
                offenders.Add($"{p.AssemblyName}: missing class {p.BuilderName}");
            }

            var hasUseMethod = types
                .Where(IsStaticClass)
                .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Static))
                .Any(m =>
                {
                    if (!string.Equals(m.Name, p.UseMethodName, StringComparison.Ordinal))
                    {
                        return false;
                    }
                    var parameters = m.GetParameters();
                    return parameters.Length >= 1
                        && string.Equals(parameters[0].ParameterType.Name, "RigBuilder", StringComparison.Ordinal);
                });

            if (!hasUseMethod)
            {
                offenders.Add($"{p.AssemblyName}: missing public static {p.UseMethodName}(this RigBuilder, ...) extension");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every provider listed in RequiredProviders must export the canonical quartet — "
                + "Fixture, FixtureOptions (with SectionName const), RigBuilder, and Use{Provider} extension. "
                + "Phase 3/4 adds providers to RequiredProviders as they reach canonical shape.");
    }

    [Test]
    public async Task NoProvider_IsBothRequiredAndSkipped()
    {
        var overlap = SkipUntilFixed
            .Select(s => s.Assembly)
            .Intersect(RequiredProviders.Select(r => r.AssemblyName), StringComparer.Ordinal)
            .ToArray();

        await Assert.That(overlap)
            .IsEmpty()
            .Because("A provider cannot be both required and skipped — move it from SkipUntilFixed into RequiredProviders");
    }

    private static bool IsStaticClass(Type t) => t is { IsClass: true, IsAbstract: true, IsSealed: true };
}
