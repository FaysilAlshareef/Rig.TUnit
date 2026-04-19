using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Rig.TUnit.Security.Fixtures;
using Rig.TUnit.Security.Policies.Options;

namespace Rig.TUnit.Security.Policies.Fixtures;

/// <summary>
/// Registers an in-process <see cref="IAuthorizationService"/> with a user-supplied
/// policy map, enabling <see cref="PolicyAssert"/> to evaluate real policy handlers
/// without spinning up Kestrel or a test-host. Per-test isolation via per-fixture
/// <see cref="IServiceProvider"/>.
/// </summary>
public sealed class PolicyFixture : SecurityFixtureBase
{
    private readonly PolicyFixtureOptions _options;
    private readonly Action<AuthorizationOptions>? _configurePolicies;
    private ServiceProvider? _provider;

    public PolicyFixture() : this(new PolicyFixtureOptions(), configurePolicies: null) { }

    public PolicyFixture(IOptions<PolicyFixtureOptions> options, Action<AuthorizationOptions>? configurePolicies = null)
        : this((options ?? throw new ArgumentNullException(nameof(options))).Value, configurePolicies) { }

    public PolicyFixture(PolicyFixtureOptions options, Action<AuthorizationOptions>? configurePolicies = null)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _configurePolicies = configurePolicies;
    }

    public IServiceProvider Services => _provider ?? throw new InvalidOperationException("Fixture not initialized.");
    public override string ConnectionString => _options.DefaultScheme;

    public override Task InitializeAsync()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddOptions();
        if (_configurePolicies is not null)
        {
            services.AddAuthorizationBuilder();
            services.AddAuthorization(_configurePolicies);
        }
        else
        {
            services.AddAuthorization();
        }
        _provider = services.BuildServiceProvider();
        return Task.CompletedTask;
    }

    public override async ValueTask DisposeAsync()
    {
        if (_provider is not null)
        {
            await _provider.DisposeAsync();
            _provider = null;
        }
    }
}
