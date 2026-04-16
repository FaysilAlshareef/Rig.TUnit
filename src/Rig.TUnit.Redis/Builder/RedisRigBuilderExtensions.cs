using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Redis.Builder;

/// <summary>
/// Entry-point extensions that wire Redis testing support into a <see cref="RigBuilder"/>.
/// </summary>
public static class RedisRigBuilderExtensions
{
    /// <summary>
    /// Registers Redis test infrastructure scoped to the supplied connection source. When
    /// <paramref name="configure"/> is <see langword="null"/> the default behavior replaces the
    /// <see cref="StackExchange.Redis.IConnectionMultiplexer"/> registration.
    /// </summary>
    public static RigBuilder UseRedis(
        this RigBuilder rigBuilder,
        IRigConnectionSource source,
        Action<RedisRigBuilder>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(rigBuilder);
        ArgumentNullException.ThrowIfNull(source);

        var builder = new RedisRigBuilder(rigBuilder.Services, source);
        if (configure is null)
        {
            builder.ReplaceMultiplexer();
        }
        else
        {
            configure(builder);
        }

        return rigBuilder;
    }
}
