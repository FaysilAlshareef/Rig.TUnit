using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Caching.Contracts;

/// <summary>Contract implemented by every cache fixture (Memory, Redis, Hybrid, Fusion).</summary>
public interface ICacheRig : IRigConnectionSource
{
    IsolationKey IsolationKey { get; }

    /// <summary>Per-test unique key prefix for all cache entries this rig owns.</summary>
    string KeyPrefix { get; }
}
