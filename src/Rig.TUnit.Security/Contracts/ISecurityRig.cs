using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Security.Contracts;

/// <summary>Contract implemented by every security fixture (Jwt, OAuth, mTLS).</summary>
public interface ISecurityRig : IRigConnectionSource
{
    IsolationKey IsolationKey { get; }
}
