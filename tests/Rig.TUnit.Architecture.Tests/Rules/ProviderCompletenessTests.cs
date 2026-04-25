using System.Reflection;
using Rig.TUnit.Architecture.Tests.Infrastructure;
using Rig.TUnit.Messaging.Helpers;
using Rig.TUnit.Messaging.Topology;

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
///
/// Feature 007 (C-005) extension: additional tests assert every assembly listed in
/// <c>.parity-coverage.txt</c> exposes <c>WithTopology(Action&lt;ITopologyBuilder&gt;)</c>,
/// a <see cref="SendContext"/>-accepting sender overload, and — for session-capable providers —
/// a concrete <c>ListenerBase&lt;&gt;</c> subtype. The file is empty at Phase 0 exit;
/// each provider phase appends its assembly name on GREEN and flips its own assertions.
/// </summary>
public sealed class ProviderCompletenessTests
{
    private sealed record ProviderEntry(
        string AssemblyName,
        string? FixtureName,
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
        new("Rig.TUnit.Databases.Sql.MySql",      "MySqlFixture",      "MySqlFixtureOptions",      "MySqlRigBuilder",      "UseMySql"),
        new("Rig.TUnit.Databases.Sql.Oracle",     "OracleFixture",     "OracleFixtureOptions",     "OracleRigBuilder",     "UseOracle"),
        new("Rig.TUnit.Databases.NoSql.Mongo",    "MongoFixture",      "MongoFixtureOptions",      "MongoRigBuilder",      "UseMongo"),
        new("Rig.TUnit.Databases.NoSql.Cosmos",   "CosmosFixture",     "CosmosFixtureOptions",     "CosmosRigBuilder",     "UseCosmos"),
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
        new("Rig.TUnit.Security.Jwt",             null,               "JwtBuilderOptions",       "JwtRigBuilder",        "UseJwt"),
        new("Rig.TUnit.Security.OAuth",           "MockOAuthServer",  "MockOAuthServerOptions",  "OAuthRigBuilder",      "UseOAuthServer"),
        new("Rig.TUnit.Security.Mtls",            "MtlsFixture",      "MtlsFixtureOptions",      "MtlsRigBuilder",       "UseMtls"),
        new("Rig.TUnit.Security.Policies",        "PolicyFixture",    "PolicyFixtureOptions",    "PolicyRigBuilder",     "UsePolicies"),
        new("Rig.TUnit.Observability.Metrics",    "MetricsFixture",   "MetricsFixtureOptions",   "MetricsRigBuilder",    "UseMetricsCapture"),
        new("Rig.TUnit.Observability.AppInsights","AppInsightsFixture","AppInsightsFixtureOptions","AppInsightsRigBuilder","UseAppInsights"),
        new("Rig.TUnit.Docker",                   "ContainerFixture", "DockerFixtureOptions",    "DockerRigBuilder",     "UseDocker"),
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
        ("Rig.TUnit.Observability.Logging",           "by-design — telemetry-style (no fluent Use extension)"),
        ("Rig.TUnit.Observability.Tracing",           "by-design — telemetry-style"),
        ("Rig.TUnit.Observability.Seq",               "by-design — telemetry-style"),
    ];

    /// <summary>
    /// Session/partition-capable messaging providers. When one of these assemblies appears in
    /// <c>.parity-coverage.txt</c>, it MUST expose a concrete <c>ListenerBase&lt;&gt;</c> subtype
    /// (the listener that populates <c>CapturedMessage.SessionKey</c>).
    /// </summary>
    private static readonly HashSet<string> SessionCapableAssemblies = new(StringComparer.Ordinal)
    {
        "Rig.TUnit.Messaging.ServiceBus",
        "Rig.TUnit.Messaging.Kafka",
        "Rig.TUnit.Messaging.Nats",
        "Rig.TUnit.Messaging.Sqs",
    };

    private const string ParityCoverageFileName = ".parity-coverage.txt";

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

            if (p.FixtureName is not null && !types.Any(t => t is { IsClass: true, IsAbstract: false } && t.Name == p.FixtureName))
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

    [Test]
    public async Task Security_ProvidersWithoutContainer_NeedNoFixture()
    {
        // Providers that ship without a container-backed Fixture (e.g., Jwt — pure token-signing,
        // no broker/server) declare FixtureName: null in RequiredProviders. This test documents
        // the intent so future contributors know nulls are not a mistake.
        var nullFixtureEntries = RequiredProviders.Where(p => p.FixtureName is null).ToArray();
        await Assert.That(nullFixtureEntries.Length).IsGreaterThanOrEqualTo(1)
            .Because("At least one provider (Jwt) intentionally ships without a container-backed Fixture.");
    }

    [Test]
    public async Task ParityCoverageFile_Exists_WithLoadableAssemblies()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ParityCoverageFileName);

        await Assert.That(File.Exists(path))
            .IsTrue()
            .Because(
                $"{ParityCoverageFileName} drives the Feature-007 parity tests. T003-GREEN creates "
                + "it empty; each provider phase appends its assembly name on GREEN.");

        foreach (var line in ReadParityCoverageLines())
        {
            var assembly = AssemblyLoader.TryLoad(line);
            await Assert.That(assembly)
                .IsNotNull()
                .Because(
                    $"Parity coverage file references '{line}' but the assembly could not be loaded. "
                    + "Either the file has a typo, or the assembly is not transitively referenced by "
                    + "Rig.TUnit.Architecture.Tests.csproj.");
        }
    }

    [Test]
    public async Task Providers_InParityCoverage_DeclareWithTopology()
    {
        var offenders = new List<string>();

        foreach (var line in ReadParityCoverageLines())
        {
            var assembly = AssemblyLoader.TryLoad(line);
            if (assembly is null)
            {
                continue;
            }

            var builder = assembly
                .GetExportedTypes()
                .FirstOrDefault(t => t.IsClass && t.Name.EndsWith("RigBuilder", StringComparison.Ordinal));

            if (builder is null)
            {
                offenders.Add($"{line}: no type whose name ends in 'RigBuilder'");
                continue;
            }

            var hasWithTopology = builder
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m =>
                {
                    if (!string.Equals(m.Name, "WithTopology", StringComparison.Ordinal))
                    {
                        return false;
                    }

                    var parameters = m.GetParameters();
                    if (parameters.Length != 1)
                    {
                        return false;
                    }

                    var paramType = parameters[0].ParameterType;
                    if (!paramType.IsGenericType || paramType.GetGenericTypeDefinition() != typeof(Action<>))
                    {
                        return false;
                    }

                    var actionArg = paramType.GetGenericArguments()[0];
                    return typeof(ITopologyBuilder).IsAssignableFrom(actionArg);
                });

            if (!hasWithTopology)
            {
                offenders.Add(
                    $"{line}.{builder.Name}: missing WithTopology(Action<T>) where T : ITopologyBuilder");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every assembly listed in .parity-coverage.txt must expose a strongly-typed "
                + "WithTopology overload on its {Provider}RigBuilder (FR-007-02).");
    }

    [Test]
    public async Task Providers_InParityCoverage_DeclareSendContextOverload()
    {
        var offenders = new List<string>();

        foreach (var line in ReadParityCoverageLines())
        {
            var assembly = AssemblyLoader.TryLoad(line);
            if (assembly is null)
            {
                continue;
            }

            var senders = assembly
                .GetExportedTypes()
                .Where(t => t.IsClass && t.Name.EndsWith("EventSender", StringComparison.Ordinal))
                .ToArray();

            if (senders.Length == 0)
            {
                offenders.Add($"{line}: no type whose name ends in 'EventSender'");
                continue;
            }

            var anySenderHasOverload = senders.Any(sender => sender
                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                .Any(m =>
                    string.Equals(m.Name, "SendAsync", StringComparison.Ordinal)
                    && m.GetParameters().Any(p => p.ParameterType == typeof(SendContext))));

            if (!anySenderHasOverload)
            {
                offenders.Add($"{line}: no EventSender with a SendAsync(..., SendContext, ...) overload");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Every assembly listed in .parity-coverage.txt must expose a SendContext-accepting "
                + "SendAsync overload on at least one of its EventSender types (FR-007-01).");
    }

    [Test]
    public async Task SessionCapableProviders_InParityCoverage_DeclareSessionListener()
    {
        var offenders = new List<string>();
        var listenerBaseOpen = typeof(Rig.TUnit.Messaging.Helpers.ListenerBase<>);

        foreach (var line in ReadParityCoverageLines())
        {
            if (!SessionCapableAssemblies.Contains(line))
            {
                continue;
            }

            var assembly = AssemblyLoader.TryLoad(line);
            if (assembly is null)
            {
                continue;
            }

            var hasListener = assembly
                .GetExportedTypes()
                .Where(t => t is { IsClass: true, IsAbstract: false })
                .Any(t => InheritsFromOpenGeneric(t, listenerBaseOpen));

            if (!hasListener)
            {
                offenders.Add($"{line}: no concrete ListenerBase<T> subtype found");
            }
        }

        await Assert.That(offenders)
            .IsEmpty()
            .Because(
                "Session/partition-capable messaging providers must declare a concrete "
                + "ListenerBase<T> subtype that populates CapturedMessage.SessionKey (FR-007-03).");
    }

    private static IEnumerable<string> ReadParityCoverageLines()
    {
        var path = Path.Combine(AppContext.BaseDirectory, ParityCoverageFileName);
        if (!File.Exists(path))
        {
            return [];
        }
        return File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0);
    }

    private static bool InheritsFromOpenGeneric(Type type, Type openGeneric)
    {
        for (var current = type; current is not null && current != typeof(object); current = current.BaseType)
        {
            if (current.IsGenericType && current.GetGenericTypeDefinition() == openGeneric)
            {
                return true;
            }
        }
        return false;
    }

    private static bool IsStaticClass(Type t) => t is { IsClass: true, IsAbstract: true, IsSealed: true };
}
