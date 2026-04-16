using Microsoft.Extensions.Options;

namespace Rig.TUnit.Core.Builder;

internal sealed class OptionsConnectionSource<TOptions>(
    IOptions<TOptions> options, Func<TOptions, string> selector) : IRigConnectionSource
    where TOptions : class
{
    public string ConnectionString => selector(options.Value)
        ?? throw new InvalidOperationException(
            $"Options selector for {typeof(TOptions).Name} returned null.");
}
