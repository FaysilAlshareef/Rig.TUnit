using System.Reflection;
using Rig.TUnit.Architecture.Tests.Infrastructure;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Enforces FR-040 + FR-043: every public non-abstract type in a Rig.TUnit.* source
/// assembly MUST be referenced by at least one Rig.TUnit.*.Tests.* assembly
/// (directly via <c>typeof</c>, field/parameter/property types, or indirectly via a
/// <c>{TypeName}Tests</c> / <c>{TypeName}Contract</c> class name).
/// Whitelist exceptions live in <c>coverage-whitelist.txt</c>.
/// </summary>
public sealed class CoverageRuleTests
{
    [Test]
    public async Task EveryPublicType_HasReferencingTestAssembly()
    {
        var whitelist = LoadWhitelist();
        var testAssemblies = AssemblyLoader.TestAssemblies;
        if (testAssemblies.Count == 0)
        {
            return;
        }

        var testTypeReferences = testAssemblies
            .SelectMany(SafeGetTypes)
            .ToArray();

        var testTypeSimpleNames = testTypeReferences
            .Select(t => t.Name)
            .ToHashSet(StringComparer.Ordinal);

        var testReferencedFullNames = testTypeReferences
            .SelectMany(CollectReferencedTypeFullNames)
            .ToHashSet(StringComparer.Ordinal);

        var uncovered = new List<string>();
        foreach (var sourceAssembly in AssemblyLoader.SourceAssemblies)
        {
            foreach (var type in SafeGetExportedTypes(sourceAssembly))
            {
                if (type is { IsAbstract: true, IsSealed: false })
                {
                    continue;
                }

                if (type.IsEnum || type.IsInterface)
                {
                    continue;
                }

                if (type.FullName is null || whitelist.Contains(type.FullName))
                {
                    continue;
                }

                var covered = testReferencedFullNames.Contains(type.FullName)
                           || testTypeSimpleNames.Contains($"{type.Name}Tests")
                           || testTypeSimpleNames.Contains($"{type.Name}Contract")
                           || testTypeSimpleNames.Contains($"{type.Name}IntegrationTests");

                if (!covered)
                {
                    uncovered.Add(type.FullName);
                }
            }
        }

        await Assert.That(uncovered)
            .IsEmpty()
            .Because("Every public concrete type must be covered by a test assembly (name convention or type reference)");
    }

    private static HashSet<string> LoadWhitelist()
    {
        var path = Path.Combine(AppContext.BaseDirectory, "coverage-whitelist.txt");
        if (!File.Exists(path))
        {
            return [];
        }

        return File.ReadAllLines(path)
            .Select(l => l.Trim())
            .Where(l => l.Length > 0 && !l.StartsWith('#'))
            .Select(l =>
            {
                var idx = l.IndexOf('#', StringComparison.Ordinal);
                return idx >= 0 ? l[..idx].Trim() : l;
            })
            .ToHashSet(StringComparer.Ordinal);
    }

    private static IEnumerable<Type> SafeGetTypes(Assembly a)
    {
        try
        {
            return a.GetTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>();
        }
    }

    private static IEnumerable<Type> SafeGetExportedTypes(Assembly a)
    {
        try
        {
            return a.GetExportedTypes();
        }
        catch (ReflectionTypeLoadException ex)
        {
            return ex.Types.OfType<Type>().Where(t => t.IsPublic);
        }
        catch (FileNotFoundException)
        {
            return [];
        }
    }

    private static IEnumerable<string> CollectReferencedTypeFullNames(Type type)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            AddTypeName(seen, field.FieldType);
        }
        foreach (var prop in type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            AddTypeName(seen, prop.PropertyType);
        }
        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static))
        {
            AddTypeName(seen, method.ReturnType);
            foreach (var p in method.GetParameters())
            {
                AddTypeName(seen, p.ParameterType);
            }
        }
        if (type.BaseType is not null)
        {
            AddTypeName(seen, type.BaseType);
        }
        foreach (var iface in type.GetInterfaces())
        {
            AddTypeName(seen, iface);
        }
        return seen;
    }

    private static void AddTypeName(HashSet<string> set, Type? t)
    {
        while (t is not null)
        {
            if (t.FullName is not null)
            {
                set.Add(t.FullName);
            }
            if (t.IsGenericType)
            {
                foreach (var arg in t.GetGenericArguments())
                {
                    AddTypeName(set, arg);
                }
            }
            t = t.HasElementType ? t.GetElementType() : null;
        }
    }
}
