using System.Reflection;
using Rig.TUnit.Architecture.Tests.Infrastructure;

namespace Rig.TUnit.Architecture.Tests.Rules;

/// <summary>
/// Detects forbidden APIs that violate <c>.claude/rules/architecture-profile.md</c> and
/// <c>async-concurrency.md</c>. Reflection-based scan across method signatures and IL method
/// references; a Roslyn analyzer provides the precise source-location companion
/// (<c>Rig.TUnit.Observability.Logging.Analyzers</c>).
/// </summary>
public sealed class ForbiddenApiTests
{
    [Test]
    public async Task NoSource_UsesDateTimeNow()
    {
        var offenders = AssemblyLoader.SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => SafeGetMethods(t))
            .Where(m => ReferencesDateTimeNowOrUtcNow(m))
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .Distinct()
            .ToArray();

        await Assert.That(offenders)
            .IsEmpty()
            .Because("Production code must use injected TimeProvider, not DateTime.Now/UtcNow");
    }

    [Test]
    public async Task NoSource_UsesAsyncVoid()
    {
        var offenders = AssemblyLoader.SourceAssemblies
            .SelectMany(a => a.GetTypes())
            .SelectMany(t => SafeGetMethods(t))
            .Where(IsAsyncVoid)
            .Select(m => $"{m.DeclaringType?.FullName}.{m.Name}")
            .ToArray();

        await Assert.That(offenders)
            .IsEmpty()
            .Because("async void is forbidden — return Task or Task<T>");
    }

    private static IEnumerable<MethodInfo> SafeGetMethods(Type t)
    {
        try
        {
            return t.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly);
        }
        catch (TypeLoadException)
        {
            return Array.Empty<MethodInfo>();
        }
        catch (ReflectionTypeLoadException)
        {
            return Array.Empty<MethodInfo>();
        }
    }

    private static bool IsAsyncVoid(MethodInfo m)
    {
        if (m.ReturnType != typeof(void))
        {
            return false;
        }

        return m.GetCustomAttributes(inherit: false)
            .Any(a => a.GetType().FullName == "System.Runtime.CompilerServices.AsyncStateMachineAttribute");
    }

    private static bool ReferencesDateTimeNowOrUtcNow(MethodInfo m)
    {
        try
        {
            var body = m.GetMethodBody();
            if (body is null)
            {
                return false;
            }

            var il = body.GetILAsByteArray();
            if (il is null || il.Length == 0)
            {
                return false;
            }

            var module = m.Module;
            for (var i = 0; i < il.Length - 4; i++)
            {
                if (il[i] != 0x28 && il[i] != 0x6F)
                {
                    continue;
                }

                var token = BitConverter.ToInt32(il, i + 1);
                try
                {
                    var member = module.ResolveMember(token);
                    if (member is MethodBase mb && mb.DeclaringType == typeof(DateTime)
                        && (mb.Name == "get_Now" || mb.Name == "get_UtcNow" || mb.Name == "get_Today"))
                    {
                        return true;
                    }
                }
                catch (ArgumentException)
                {
                }
                catch (MissingMemberException)
                {
                }
            }
            return false;
        }
        catch (InvalidOperationException)
        {
            return false;
        }
    }
}
