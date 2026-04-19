using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Rig.TUnit.Core.Fixtures;

namespace Rig.TUnit.Docker.Fixtures;

/// <summary>
/// Generic Testcontainer-backed fixture. Boots a single container with the
/// user-supplied image + env + exposed ports. Useful when the provider-specific
/// fixtures (SqlServer/Redis/...) are not a fit.
/// </summary>
public sealed class ContainerFixture : RigFixtureBase
{
    private readonly string _image;
    private readonly IReadOnlyDictionary<string, string>? _env;
    private readonly IReadOnlyCollection<int>? _exposedPorts;
    private IContainer? _container;

    public ContainerFixture(string image, IReadOnlyDictionary<string, string>? env = null, IReadOnlyCollection<int>? exposedPorts = null)
    {
        _image = image ?? throw new ArgumentNullException(nameof(image));
        _env = env;
        _exposedPorts = exposedPorts;
    }

    public IContainer Container => _container ?? throw new InvalidOperationException("Initialize first.");

    public override string ConnectionString
    {
        get
        {
            if (_container is null) throw new InvalidOperationException("Initialize first.");
            return _container.Hostname;
        }
    }

    public override async Task InitializeAsync()
    {
        if (_container is not null) return;

        var builder = new ContainerBuilder(_image);
        if (_env is not null) foreach (var kv in _env) builder = builder.WithEnvironment(kv.Key, kv.Value);
        if (_exposedPorts is not null) foreach (var p in _exposedPorts) builder = builder.WithPortBinding(p, assignRandomHostPort: true);

        _container = builder.Build();
        using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
        await _container.StartAsync(cts.Token);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_container is not null)
        {
            await _container.DisposeAsync();
            _container = null;
        }
    }
}
