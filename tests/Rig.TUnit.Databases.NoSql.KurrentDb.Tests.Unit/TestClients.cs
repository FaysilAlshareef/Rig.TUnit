using KurrentDB.Client;

namespace Rig.TUnit.Databases.NoSql.KurrentDb.Tests.Unit;

/// <summary>
/// Helper that constructs a <see cref="KurrentDBClient"/> pointed at an unreachable
/// address (RFC-5737 TEST-NET-1). Used by guard-only unit tests — the client never
/// issues a network call because the helpers short-circuit on argument validation.
/// </summary>
internal static class TestClients
{
    public static KurrentDBClient Offline()
    {
        var settings = KurrentDBClientSettings.Create("esdb://192.0.2.1:2113?tls=false");
        return new KurrentDBClient(settings);
    }
}
