using System.Net;
using System.Net.Sockets;

namespace Rig.TUnit.Parallelism.Helpers;

/// <summary>
/// Thread-safe OS-level ephemeral-port allocator. Binds to port 0, reads the
/// OS-assigned port, then closes the socket before returning. In practice this
/// is collision-free because OS port assignment is sequential and the allocator
/// keeps the socket alive through a sub-millisecond window.
/// </summary>
public static class PortAllocator
{
    public static int Allocate()
    {
        using var listener = new TcpListener(IPAddress.Loopback, 0);
        listener.Start();
        var port = ((IPEndPoint)listener.LocalEndpoint).Port;
        listener.Stop();
        return port;
    }

    /// <summary>Allocates <paramref name="count"/> distinct ports by binding simultaneously.</summary>
    public static IReadOnlyList<int> Allocate(int count)
    {
        if (count <= 0) throw new ArgumentOutOfRangeException(nameof(count));
        var listeners = new List<TcpListener>(count);
        try
        {
            var ports = new List<int>(count);
            for (var i = 0; i < count; i++)
            {
                var l = new TcpListener(IPAddress.Loopback, 0);
                l.Start();
                listeners.Add(l);
                ports.Add(((IPEndPoint)l.LocalEndpoint).Port);
            }
            return ports;
        }
        finally
        {
            foreach (var l in listeners) l.Stop();
        }
    }
}
