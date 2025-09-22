namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Test implementation of IUIProfile for use in unit tests.
/// Provides a simple, configurable profile implementation for testing scenarios.
/// </summary>
internal class TestUIProfile : IUIProfile
{
    private UIProfileMetadata _metadata;
    private CommandSet? _commandSet;
    private LayoutConfiguration? _layoutConfiguration;
    private KeyBindingSet? _keyBindings;

    public TestUIProfile(string name, ConsoleMode targetMode)
    {
        Name = name;
        TargetMode = targetMode;
        _metadata = new UIProfileMetadata
        {
            Version = "1.0.0",
            DisplayName = $"{name} Profile",
            Description = $"Test profile for {targetMode} mode",
            Author = "Test Suite"
        };
    }

    public string Name { get; }
    public ConsoleMode TargetMode { get; }
    public UIProfileMetadata Metadata => _metadata;

    public CommandSet GetCommandSet()
    {
        if (_commandSet == null)
        {
            _commandSet = new CommandSet();
            _commandSet.AddCommand("test-command", new TestCommandDefinition("test-command", "Test Command"));
        }
        return _commandSet;
    }

    public LayoutConfiguration GetLayoutConfiguration()
    {
        if (_layoutConfiguration == null)
        {
            _layoutConfiguration = new LayoutConfiguration
            {
                MainWindow = new WindowConfiguration
                {
                    Title = $"{Name} Window",
                    Width = 1024,
                    Height = 768
                }
            };
        }
        return _layoutConfiguration;
    }

    public KeyBindingSet GetKeyBindings()
    {
        if (_keyBindings == null)
        {
            _keyBindings = new KeyBindingSet();
            _keyBindings.AddBinding("Ctrl+T", "test-command");
        }
        return _keyBindings;
    }

    public Task<bool> CanActivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Simple validation - profile can activate if target mode matches context mode
        return Task.FromResult(TargetMode == context.CurrentMode);
    }

    public virtual Task ActivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Simulate activation work
        return Task.CompletedTask;
    }

    public Task DeactivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Simulate deactivation work
        return Task.CompletedTask;
    }

    public Task SaveConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Simulate configuration persistence
        return Task.CompletedTask;
    }

    public Task ReloadConfigurationAsync(CancellationToken cancellationToken = default)
    {
        // Simulate configuration reload
        return Task.CompletedTask;
    }

    // Helper method for testing
    internal void SetMetadata(UIProfileMetadata metadata)
    {
        _metadata = metadata;
    }
}

/// <summary>
/// Test implementation of IUIContext for use in unit tests.
/// </summary>
internal class TestUIContext : IUIContext
{
    private readonly Dictionary<string, object> _properties = new();

    public TestUIContext(ConsoleMode currentMode)
    {
        CurrentMode = currentMode;
        Services = new TestServiceProvider();
        ShutdownToken = CancellationToken.None;
    }

    public ConsoleMode CurrentMode { get; }
    public IServiceProvider Services { get; }
    public IReadOnlyDictionary<string, object> Properties => _properties;
    public CancellationToken ShutdownToken { get; }

    public void SetProperty(string key, object value)
    {
        _properties[key] = value;
    }
}

/// <summary>
/// Test implementation of ICommandDefinition for use in unit tests.
/// </summary>
internal class TestCommandDefinition : ICommandDefinition
{
    public TestCommandDefinition(string name, string description, string category = "Test", bool isEnabled = true)
    {
        Name = name;
        Description = description;
        Category = category;
        IsEnabled = isEnabled;
    }

    public string Name { get; }
    public string Description { get; }
    public string Category { get; }
    public bool IsEnabled { get; }
}

/// <summary>
/// Test implementation of IUIProfile that simulates slow activation for concurrency testing.
/// </summary>
internal class SlowActivationTestUIProfile : TestUIProfile
{
    public SlowActivationTestUIProfile(string name, ConsoleMode targetMode) : base(name, targetMode)
    {
    }

    public override async Task ActivateAsync(IUIContext context, CancellationToken cancellationToken = default)
    {
        // Simulate slow activation to test concurrency control
        await Task.Delay(100, cancellationToken);
        await base.ActivateAsync(context, cancellationToken);
    }
}

/// <summary>
/// Simple test service provider implementation.
/// </summary>
internal class TestServiceProvider : IServiceProvider
{
    private readonly Dictionary<Type, object> _services = new();

    public object? GetService(Type serviceType)
    {
        return _services.GetValueOrDefault(serviceType);
    }

    public void AddService<T>(T service) where T : notnull
    {
        _services[typeof(T)] = service;
    }
}