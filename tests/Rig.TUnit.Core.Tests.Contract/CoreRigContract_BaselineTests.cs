namespace Rig.TUnit.Core.Tests.Contract;

/// <summary>
/// Concrete inheritor of <see cref="CoreRigContract"/> using the vanilla
/// <see cref="Microsoft.Extensions.DependencyInjection.ServiceCollection"/> baseline.
/// Provider-specific contract suites (SqlServer, Mongo, Redis, …) supply their own
/// derivation via <c>[InheritsTests]</c> on top of a preconfigured service collection.
/// </summary>
[InheritsTests]
public sealed class CoreRigContract_BaselineTests : CoreRigContract
{
}
