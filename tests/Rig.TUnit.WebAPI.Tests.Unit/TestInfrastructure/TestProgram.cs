namespace Rig.TUnit.WebAPI.Tests.Unit.TestInfrastructure;

/// <summary>
/// Marker class for <see cref="Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory{TEntryPoint}"/>
/// generic constraints. Has no <c>Main</c> method because TUnit disallows one; the host is built by
/// <see cref="TestWebApplicationFactory"/>.
/// </summary>
public sealed class TestProgram;
