using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Rig.TUnit.WebAPI.Extensions;

namespace Rig.TUnit.WebAPI.Authentication;

/// <summary>
/// Authentication and authorization helpers for <see cref="WebApplicationFactory{TEntryPoint}"/>.
/// </summary>
public static class TestAuthenticationExtensions
{
    /// <summary>
    /// Replaces the application's authentication pipeline with a test scheme that authenticates every
    /// request using the supplied <paramref name="configureOptions"/>. Use to bypass real identity providers
    /// in tests without changing production code.
    /// </summary>
    public static WebApplicationFactory<TProgram> WithTestAuthentication<TProgram>(
        this WebApplicationFactory<TProgram> factory,
        Action<TestAuthenticationOptions>? configureOptions = null) where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.WithTestServices(services =>
        {
            services
                .AddAuthentication(TestAuthenticationHandler.SchemeName)
                .AddScheme<TestAuthenticationOptions, TestAuthenticationHandler>(
                    TestAuthenticationHandler.SchemeName,
                    options => configureOptions?.Invoke(options));

            services.Configure<AuthenticationOptions>(authOptions =>
            {
                authOptions.DefaultAuthenticateScheme = TestAuthenticationHandler.SchemeName;
                authOptions.DefaultChallengeScheme = TestAuthenticationHandler.SchemeName;
            });
        });
    }

    /// <summary>
    /// Replaces the authorization pipeline's <see cref="AuthorizationOptions.DefaultPolicy"/> and
    /// <see cref="AuthorizationOptions.FallbackPolicy"/> with a policy that only requires an authenticated user
    /// against the <see cref="TestAuthenticationHandler.SchemeName"/> scheme. Intended for tests that exercise
    /// business logic rather than authorization.
    /// <para>
    /// NOTE: Only default and fallback policies are overridden. Named policies applied via
    /// <c>[Authorize(Policy = "...")]</c> and role requirements applied via <c>[Authorize(Roles = "...")]</c>
    /// are not bypassed — configure the corresponding policies or claims explicitly if your endpoints use them.
    /// </para>
    /// </summary>
    public static WebApplicationFactory<TProgram> WithPermissiveAuthorization<TProgram>(
        this WebApplicationFactory<TProgram> factory) where TProgram : class
    {
        ArgumentNullException.ThrowIfNull(factory);

        return factory.WithTestServices(services =>
        {
            services.AddAuthorization(options =>
            {
                options.DefaultPolicy = new AuthorizationPolicyBuilder()
                    .AddAuthenticationSchemes(TestAuthenticationHandler.SchemeName)
                    .RequireAuthenticatedUser()
                    .Build();
                options.FallbackPolicy = options.DefaultPolicy;
            });
        });
    }
}
