using Azure.Messaging.ServiceBus;

namespace Rig.TUnit.Messaging.ServiceBus.Tests.Unit;

/// <summary>
/// Builds a <see cref="ServiceBusClient"/> whose endpoint is guaranteed non-routable
/// (RFC-5737 TEST-NET-1). Used by guard-only unit tests — helpers short-circuit on
/// argument validation before the client ever issues a network call.
/// </summary>
internal static class TestClients
{
    private const string OfflineConnectionString =
        "Endpoint=sb://192.0.2.1.servicebus.windows.net/;SharedAccessKeyName=k;SharedAccessKey=dGVzdA==";

    public static ServiceBusClient Offline() => new(OfflineConnectionString);
}
