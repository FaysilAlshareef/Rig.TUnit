using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Storage.AzureBlob.Builder;

public static class AzureBlobRigBuilderExtensions
{
    public static RigBuilder UseAzureBlob(
        this RigBuilder rig,
        IRigConnectionSource source,
        Action<AzureBlobRigBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(rig);
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(configure);

        var builder = new AzureBlobRigBuilder(rig, source);
        configure(builder);
        return rig;
    }
}
