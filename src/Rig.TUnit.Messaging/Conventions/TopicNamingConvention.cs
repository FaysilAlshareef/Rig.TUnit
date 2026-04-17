using Rig.TUnit.Core;

namespace Rig.TUnit.Messaging.Conventions;

/// <summary>
/// Default topic naming: <c>{company}-{domain}-{side}</c> lowercase, with the test's
/// isolation key appended so parallel fixtures do not collide.
/// </summary>
public sealed class TopicNamingConvention
{
    private TopicNamingConvention() { }

    public static string Default(IsolationKey key, string company = "rigtunit", string domain = "test", string side = "events")
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(company);
        ArgumentException.ThrowIfNullOrWhiteSpace(domain);
        ArgumentException.ThrowIfNullOrWhiteSpace(side);
        return $"{company}-{domain}-{side}-{key.Value}".ToLowerInvariant();
    }
}
