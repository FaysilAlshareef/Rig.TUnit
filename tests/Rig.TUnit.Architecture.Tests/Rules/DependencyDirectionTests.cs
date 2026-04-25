using NetArchTest.Rules;
using Rig.TUnit.Architecture.Tests.Infrastructure;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Enforces the Base + Provider dependency direction across the Rig.TUnit ecosystem.
/// Rules activate progressively as packages land — a rule silently passes when its
/// target assembly has not yet been built (phase-by-phase rollout).
/// </summary>
public sealed class DependencyDirectionTests
{
    [Test]
    public async Task Databases_DoesNotReferenceAnySqlOrNoSqlProvider()
    {
        var assembly = AssemblyLoader.TryLoad("Rig.TUnit.Databases");
        if (assembly is null)
        {
            return;
        }

        var forbiddenPrefixes = new[]
        {
            "Rig.TUnit.Databases.Sql",
            "Rig.TUnit.Databases.NoSql",
        };

        var result = Types.InAssembly(assembly)
            .That()
            .HaveDependencyOnAny(forbiddenPrefixes)
            .GetTypes()
            .ToArray();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task DatabasesSql_DoesNotReferenceAnyProvider()
    {
        var assembly = AssemblyLoader.TryLoad("Rig.TUnit.Databases.Sql");
        if (assembly is null)
        {
            return;
        }

        var forbidden = new[]
        {
            "Rig.TUnit.Databases.Sql.SqlServer",
            "Rig.TUnit.Databases.Sql.Sqlite",
            "Rig.TUnit.Databases.Sql.Postgresql",
            "Rig.TUnit.Databases.Sql.MySql",
            "Rig.TUnit.Databases.Sql.Oracle",
        };

        var result = Types.InAssembly(assembly)
            .That()
            .HaveDependencyOnAny(forbidden)
            .GetTypes()
            .ToArray();

        await Assert.That(result).IsEmpty();
    }

    [Test]
    public async Task Providers_DoNotReferenceSiblings()
    {
        var sqlProviders = new[]
        {
            "Rig.TUnit.Databases.Sql.SqlServer",
            "Rig.TUnit.Databases.Sql.Sqlite",
            "Rig.TUnit.Databases.Sql.Postgresql",
            "Rig.TUnit.Databases.Sql.MySql",
            "Rig.TUnit.Databases.Sql.Oracle",
        };

        foreach (var provider in sqlProviders)
        {
            var assembly = AssemblyLoader.TryLoad(provider);
            if (assembly is null)
            {
                continue;
            }

            var siblings = sqlProviders.Where(s => s != provider).ToArray();
            var result = Types.InAssembly(assembly)
                .That()
                .HaveDependencyOnAny(siblings)
                .GetTypes()
                .ToArray();

            await Assert.That(result)
                .IsEmpty()
                .Because($"{provider} must not reference sibling SQL provider");
        }
    }

    [Test]
    public async Task Microservices_DependOnlyOnBases()
    {
        var microservicePackages = new[]
        {
            "Rig.TUnit.Microservices.Outbox",
            "Rig.TUnit.Microservices.Inbox",
            "Rig.TUnit.Microservices.EventSourcing",
            "Rig.TUnit.Microservices.Snapshots",
            "Rig.TUnit.Microservices.Saga",
            "Rig.TUnit.Microservices.Contracts",
        };

        var forbidden = new[]
        {
            "Rig.TUnit.Databases.Sql.SqlServer",
            "Rig.TUnit.Databases.Sql.Sqlite",
            "Rig.TUnit.Databases.Sql.Postgresql",
            "Rig.TUnit.Databases.Sql.MySql",
            "Rig.TUnit.Databases.Sql.Oracle",
            "Rig.TUnit.Databases.NoSql.Redis",
            "Rig.TUnit.Databases.NoSql.Cosmos",
            "Rig.TUnit.Databases.NoSql.Mongo",
            "Rig.TUnit.Messaging.ServiceBus",
            "Rig.TUnit.Messaging.Kafka",
            "Rig.TUnit.Messaging.RabbitMq",
            "Rig.TUnit.Caching.Redis",
            "Rig.TUnit.Caching.Memory",
            "Rig.TUnit.Caching.Hybrid",
            "Rig.TUnit.Caching.Fusion",
        };

        foreach (var pkg in microservicePackages)
        {
            var assembly = AssemblyLoader.TryLoad(pkg);
            if (assembly is null)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .That()
                .HaveDependencyOnAny(forbidden)
                .GetTypes()
                .ToArray();

            await Assert.That(result)
                .IsEmpty()
                .Because($"{pkg} must depend only on base packages, not concrete providers");
        }
    }

    [Test]
    public async Task NatsJetStream_ReferencedOnlyByNatsProvider()
    {
        var nonNatsProviders = new[]
        {
            "Rig.TUnit.Messaging.ServiceBus",
            "Rig.TUnit.Messaging.Kafka",
            "Rig.TUnit.Messaging.Sqs",
            "Rig.TUnit.Messaging.RabbitMq",
        };

        foreach (var provider in nonNatsProviders)
        {
            var assembly = AssemblyLoader.TryLoad(provider);
            if (assembly is null)
            {
                continue;
            }

            var result = Types.InAssembly(assembly)
                .That()
                .HaveDependencyOn("NATS.Client.JetStream")
                .GetTypes()
                .ToArray();

            await Assert.That(result)
                .IsEmpty()
                .Because($"{provider} must not depend on NATS.Client.JetStream");
        }
    }
}
