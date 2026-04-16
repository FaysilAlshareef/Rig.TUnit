using TUnit.Core.Interfaces;

namespace Rig.TUnit.Core.Fixtures;

/// <summary>
/// Composes multiple fixtures into one. Initializes in parallel, disposes in reverse order (LIFO).
/// </summary>
public sealed class CompositeFixture : IAsyncInitializer, IAsyncDisposable
{
    private readonly IReadOnlyList<object> _fixtures;

    public CompositeFixture(params object[] fixtures)
    {
        _fixtures = fixtures;
    }

    /// <summary>Get a fixture of a specific type from the composition.</summary>
    public T Get<T>() where T : class
        => _fixtures.OfType<T>().FirstOrDefault()
           ?? throw new InvalidOperationException(
               $"No fixture of type {typeof(T).Name} in this composition.");

    /// <summary>Initialize all fixtures that implement IAsyncInitializer, in parallel.</summary>
    public async Task InitializeAsync()
    {
        var tasks = _fixtures
            .OfType<IAsyncInitializer>()
            .Select(f => f.InitializeAsync());
        await Task.WhenAll(tasks);
    }

    /// <summary>Dispose all fixtures in reverse order (LIFO).</summary>
    public async ValueTask DisposeAsync()
    {
        foreach (var fixture in _fixtures.Reverse())
        {
            if (fixture is IAsyncDisposable disposable)
                await disposable.DisposeAsync();
        }
    }
}
