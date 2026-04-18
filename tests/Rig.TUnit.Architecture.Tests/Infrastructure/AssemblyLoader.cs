using System.Reflection;

namespace Rig.TUnit.Architecture.Tests.Infrastructure;

/// <summary>
/// Loads Rig.TUnit.* assemblies from the local build output so architecture rules can
/// reflect over every shipped package. Returns an empty array for assemblies that have
/// not yet been produced — rules that depend on a missing package silently skip until
/// the package lands (phase-by-phase rollout).
/// </summary>
public static class AssemblyLoader
{
    private static readonly Lazy<IReadOnlyList<Assembly>> _sourceAssemblies = new(LoadSourceAssemblies);
    private static readonly Lazy<IReadOnlyList<Assembly>> _testAssemblies = new(LoadTestAssemblies);

    public static IReadOnlyList<Assembly> SourceAssemblies => _sourceAssemblies.Value;

    public static IReadOnlyList<Assembly> TestAssemblies => _testAssemblies.Value;

    public static Assembly? TryLoad(string simpleName)
    {
        try
        {
            return System.Reflection.Assembly.Load(simpleName);
        }
        catch (FileNotFoundException)
        {
            return null;
        }
        catch (FileLoadException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }

    private static IReadOnlyList<Assembly> LoadSourceAssemblies()
    {
        var seeds = new[]
        {
            "Rig.TUnit.Core",
            "Rig.TUnit.Mediator",
            "Rig.TUnit.Grpc",
            "Rig.TUnit.WebAPI",
            "Rig.TUnit.Databases",
            "Rig.TUnit.Databases.Sql",
            "Rig.TUnit.Databases.Sql.SqlServer",
            "Rig.TUnit.Databases.Sql.Sqlite",
            "Rig.TUnit.Databases.Sql.Postgresql",
            "Rig.TUnit.Databases.Sql.MySql",
            "Rig.TUnit.Databases.Sql.Oracle",
            "Rig.TUnit.Databases.NoSql",
            "Rig.TUnit.Databases.NoSql.Redis",
            "Rig.TUnit.Databases.NoSql.Cosmos",
            "Rig.TUnit.Databases.NoSql.Mongo",
            "Rig.TUnit.Databases.NoSql.Dynamo",
            "Rig.TUnit.Databases.NoSql.Cassandra",
            "Rig.TUnit.Databases.NoSql.KurrentDb",
            "Rig.TUnit.Databases.NoSql.ElasticSearch",
            "Rig.TUnit.Messaging",
            "Rig.TUnit.Messaging.ServiceBus",
            "Rig.TUnit.Messaging.Kafka",
            "Rig.TUnit.Messaging.RabbitMq",
            "Rig.TUnit.Messaging.Sqs",
            "Rig.TUnit.Messaging.Nats",
            "Rig.TUnit.Caching",
            "Rig.TUnit.Caching.Memory",
            "Rig.TUnit.Caching.Redis",
            "Rig.TUnit.Caching.Hybrid",
            "Rig.TUnit.Caching.Fusion",
            "Rig.TUnit.Observability",
            "Rig.TUnit.Observability.Tracing",
            "Rig.TUnit.Observability.Logging",
            "Rig.TUnit.Observability.Logging.Analyzers",
            "Rig.TUnit.Observability.Seq",
            "Rig.TUnit.Observability.Metrics",
            "Rig.TUnit.Observability.AppInsights",
            "Rig.TUnit.Security",
            "Rig.TUnit.Security.Jwt",
            "Rig.TUnit.Security.OAuth",
            "Rig.TUnit.Security.Mtls",
            "Rig.TUnit.Security.Policies",
            "Rig.TUnit.Http",
            "Rig.TUnit.Resilience",
            "Rig.TUnit.HealthChecks",
            "Rig.TUnit.Concurrency",
            "Rig.TUnit.Docker",
            "Rig.TUnit.Parallelism",
            "Rig.TUnit.Ci",
            "Rig.TUnit.Storage",
            "Rig.TUnit.Storage.AzureBlob",
            "Rig.TUnit.Storage.S3",
            "Rig.TUnit.Storage.MinIO",
            "Rig.TUnit.Storage.FileSystem",
            "Rig.TUnit.Microservices.Outbox",
            "Rig.TUnit.Microservices.Inbox",
            "Rig.TUnit.Microservices.EventSourcing",
            "Rig.TUnit.Microservices.Snapshots",
            "Rig.TUnit.Microservices.Saga",
            "Rig.TUnit.Microservices.Contracts",
            "Rig.TUnit",
            "Rig.TUnit.Microservices",
            "Rig.TUnit.All",
        };
        return seeds.Select(TryLoad).Where(a => a is not null).Select(a => a!).ToArray();
    }

    private static IReadOnlyList<Assembly> LoadTestAssemblies()
    {
        var baseDir = AppContext.BaseDirectory;
        return Directory
            .EnumerateFiles(baseDir, "Rig.TUnit.*.Tests.*.dll", SearchOption.TopDirectoryOnly)
            .Select(TryLoadFromPath)
            .Where(a => a is not null)
            .Select(a => a!)
            .ToArray();
    }

    private static Assembly? TryLoadFromPath(string path)
    {
        try
        {
            return System.Reflection.Assembly.LoadFrom(path);
        }
        catch (FileLoadException)
        {
            return null;
        }
        catch (BadImageFormatException)
        {
            return null;
        }
    }
}
