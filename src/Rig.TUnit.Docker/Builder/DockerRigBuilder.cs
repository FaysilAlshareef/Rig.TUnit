using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Docker.Builder;

/// <summary>
/// Docker rig builder — the Docker package ships its own fluent surface (no
/// family base) since it's a generic container-runner, not a domain-shaped
/// provider like SQL/NoSQL/Messaging.
/// </summary>
public sealed class DockerRigBuilder
{
    public DockerRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    public RigBuilder Root { get; }
    public IRigConnectionSource Source { get; }

    public string Image => Source.ConnectionString;

    public RigBuilder And() => Root;
}
