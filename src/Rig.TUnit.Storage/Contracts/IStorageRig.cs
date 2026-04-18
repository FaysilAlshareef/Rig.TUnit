using Rig.TUnit.Core;
using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.Contracts;

public interface IStorageRig : IRigConnectionSource
{
    IsolationKey IsolationKey { get; }
    string ContainerName { get; }
}
