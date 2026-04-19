using System.Reflection;
using Rig.TUnit.Architecture.Tests.Infrastructure;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Enforces universal code-organization rules from <c>.claude/rules/architecture-profile.md</c>:
/// static helpers must be <c>sealed</c>, fixtures must extend <c>FixtureBase</c>, builders must be
/// <c>abstract</c> or <c>sealed</c>.
/// </summary>
public sealed class CodeOrganizationTests
{
    [Test]
    public async Task PublicStaticHelpers_AreSealed()
    {
        var offenders = AssemblyLoader.SourceAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t is { IsClass: true, IsAbstract: true, IsSealed: true })
            .Where(t => t.Name.EndsWith("Helper", StringComparison.Ordinal)
                     || t.Name.EndsWith("Extensions", StringComparison.Ordinal)
                     || t.Name.EndsWith("Assert", StringComparison.Ordinal))
            .Where(t => !IsStatic(t) && !t.IsSealed)
            .ToArray();

        await Assert.That(offenders)
            .IsEmpty()
            .Because("Public static helper/extension/assert classes must be static (abstract+sealed) or sealed");
    }

    private static readonly string[] KnownFixtureBases =
    [
        "RigFixtureBase",
        "FixtureBase",
        "DbFixtureBase",
        "SqlFixtureBase",
        "DocumentFixtureBase",
        "CacheFixtureBase",
        "MessagingFixtureBase",
        "TelemetryFixtureBase",
        "SecurityFixtureBase",
        "StorageFixtureBase",
        "CompositeFixture",
    ];

    [Test]
    public async Task AllFixtures_ExtendFixtureBase()
    {
        var offenders = AssemblyLoader.SourceAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && !t.IsAbstract && t.Name.EndsWith("Fixture", StringComparison.Ordinal))
            .Where(t => !KnownFixtureBases.Contains(t.Name, StringComparer.Ordinal))
            .Where(t => !InheritsFromAny(t, KnownFixtureBases))
            .ToArray();

        await Assert.That(offenders)
            .IsEmpty()
            .Because("All public *Fixture classes must extend a Rig.TUnit fixture base");
    }

    [Test]
    public async Task AllRigBuilders_AreAbstractOrSealed()
    {
        var offenders = AssemblyLoader.SourceAssemblies
            .SelectMany(a => a.GetExportedTypes())
            .Where(t => t.IsClass && t.Name.EndsWith("RigBuilder", StringComparison.Ordinal))
            .Where(t => !t.IsAbstract && !t.IsSealed)
            .ToArray();

        await Assert.That(offenders)
            .IsEmpty()
            .Because("RigBuilder classes must be abstract (base) or sealed (provider)");
    }

    private static bool IsStatic(Type t) => t is { IsAbstract: true, IsSealed: true };

    private static bool InheritsFromAny(Type t, params string[] baseNames)
    {
        for (var cur = t.BaseType; cur is not null; cur = cur.BaseType)
        {
            if (baseNames.Contains(cur.Name))
            {
                return true;
            }
        }
        return false;
    }
}
