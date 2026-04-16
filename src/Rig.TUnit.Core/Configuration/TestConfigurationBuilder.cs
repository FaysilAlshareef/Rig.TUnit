using Microsoft.Extensions.Configuration;

namespace Rig.TUnit.Core.Configuration;

/// <summary>
/// Builds an IConfiguration instance from in-memory key-value pairs.
/// Useful for tests that need configuration without appsettings files.
/// </summary>
public sealed class TestConfigurationBuilder
{
    private readonly Dictionary<string, string?> _values = new();

    public TestConfigurationBuilder Set(string key, string value)
    {
        _values[key] = value;
        return this;
    }

    public TestConfigurationBuilder SetConnectionString(string name, string value)
    {
        _values[$"ConnectionStrings:{name}"] = value;
        return this;
    }

    public TestConfigurationBuilder SetSection(string sectionName, Dictionary<string, string> values)
    {
        foreach (var (key, value) in values)
            _values[$"{sectionName}:{key}"] = value;
        return this;
    }

    public IConfiguration Build()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(_values!)
            .Build();
    }

    public TOptions BuildOptions<TOptions>(string sectionName) where TOptions : class, new()
    {
        var config = Build();
        var options = new TOptions();
        config.GetSection(sectionName).Bind(options);
        return options;
    }

    public static IConfiguration Create(Action<TestConfigurationBuilder> configure)
    {
        var builder = new TestConfigurationBuilder();
        configure(builder);
        return builder.Build();
    }
}
