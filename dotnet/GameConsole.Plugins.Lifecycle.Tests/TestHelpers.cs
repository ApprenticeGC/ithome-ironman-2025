using GameConsole.Plugins.Core;

namespace GameConsole.Plugins.Lifecycle.Tests;

/// <summary>
/// Mock plugin with configurable dependencies for testing.
/// </summary>
internal class MockPluginWithDependencies : IPlugin
{
    private readonly MockPluginMetadataWithDependencies _metadata;
    private bool _isRunning;

    public MockPluginWithDependencies(string pluginPath, string[] dependencies)
    {
        var fileName = Path.GetFileNameWithoutExtension(pluginPath);
        _metadata = new MockPluginMetadataWithDependencies(fileName, dependencies);
    }

    public IPluginMetadata Metadata => _metadata;
    public IPluginContext? Context { get; set; }
    public bool IsRunning => _isRunning;

    public Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context;
        return Task.CompletedTask;
    }

    public Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(!_isRunning);
    }

    public Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public ValueTask DisposeAsync()
    {
        _isRunning = false;
        return ValueTask.CompletedTask;
    }
}

internal class MockPluginMetadataWithDependencies : IPluginMetadata
{
    public MockPluginMetadataWithDependencies(string name, string[] dependencies)
    {
        Id = $"mock.{name.ToLowerInvariant()}";
        Name = name;
        Version = new Version(1, 0, 0);
        Description = $"Mock plugin for {name}";
        Author = "GameConsole";
        Dependencies = dependencies;
        Properties = new Dictionary<string, object>();
    }

    public string Id { get; }
    public string Name { get; }
    public Version Version { get; }
    public string Description { get; }
    public string Author { get; }
    public IReadOnlyList<string> Dependencies { get; }
    public IReadOnlyDictionary<string, object> Properties { get; }
}