using System.Runtime.CompilerServices;
using Bogus;

namespace Rig.TUnit.Core.Fakers;

/// <summary>
/// Bogus faker that bypasses constructors using RuntimeHelpers.GetUninitializedObject.
/// Essential for domain objects with private/protected setters.
/// Designed for inheritance — consumers create derived fakers with custom property rules.
/// </summary>
public class CustomConstructorFaker<T> : Faker<T> where T : class
{
    public CustomConstructorFaker()
    {
        CustomInstantiator(_ =>
            RuntimeHelpers.GetUninitializedObject(typeof(T)) as T
            ?? throw new TypeLoadException($"Cannot create instance of {typeof(T).Name}"));
    }
}
