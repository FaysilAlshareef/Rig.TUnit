using Rig.TUnit.Core.Builder;

namespace Rig.TUnit.Databases.Builder;

/// <summary>
/// Abstract base for every database-area rig builder. Concrete providers inherit
/// <see cref="DatabaseRigBuilder{TSelf}"/> and add provider-specific fluent methods.
/// </summary>
/// <typeparam name="TSelf">The concrete builder type, enabling fluent self-return.</typeparam>
public abstract class DatabaseRigBuilder<TSelf> where TSelf : DatabaseRigBuilder<TSelf>
{
    protected DatabaseRigBuilder(RigBuilder root, IRigConnectionSource source)
    {
        Root = root ?? throw new ArgumentNullException(nameof(root));
        Source = source ?? throw new ArgumentNullException(nameof(source));
    }

    protected RigBuilder Root { get; }

    protected IRigConnectionSource Source { get; }

    /// <summary>Returns to the root <see cref="RigBuilder"/> to continue fluent configuration.</summary>
    public RigBuilder And() => Root;
}
