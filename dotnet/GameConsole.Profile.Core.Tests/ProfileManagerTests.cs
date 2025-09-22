using GameConsole.Profile.Core;
using Microsoft.Extensions.Logging;
using Xunit;

namespace GameConsole.Profile.Core.Tests;

/// <summary>
/// Tests for the ProfileManager class.
/// </summary>
public class ProfileManagerTests
{
    private readonly IServiceProvider _mockServiceProvider;
    private readonly ILogger<ProfileManager> _mockLogger;

    public ProfileManagerTests()
    {
        _mockServiceProvider = new MockServiceProvider();
        _mockLogger = new MockLogger<ProfileManager>();
    }

    [Fact]
    public async Task InitializeAsync_RegistersBuiltInProfiles()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);

        // Act
        await manager.InitializeAsync();

        // Assert
        var availableProfiles = await manager.GetAvailableProfiles();
        var profileIds = availableProfiles.Select(p => p.ProfileId).ToList();
        
        Assert.Contains("tui", profileIds);
        Assert.Contains("unity", profileIds);
        Assert.Contains("godot", profileIds);
    }

    [Fact]
    public async Task StartAsync_ActivatesBestProfile()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();

        // Act
        await manager.StartAsync();

        // Assert
        Assert.NotNull(manager.ActiveProfile);
        Assert.Equal("tui", manager.ActiveProfile.ProfileId); // TUI has highest priority
        Assert.True(manager.IsRunning);
    }

    [Fact]
    public async Task ActivateProfile_ValidProfile_ReturnsTrue()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();

        // Act
        var result = await manager.ActivateProfile("unity");

        // Assert
        Assert.True(result);
        Assert.NotNull(manager.ActiveProfile);
        Assert.Equal("unity", manager.ActiveProfile.ProfileId);
    }

    [Fact]
    public async Task ActivateProfile_InvalidProfile_ReturnsFalse()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();

        // Act
        var result = await manager.ActivateProfile("nonexistent");

        // Assert
        Assert.False(result);
        Assert.Null(manager.ActiveProfile);
    }

    [Fact]
    public async Task ActivateProfile_RaisesProfileChangedEvent()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();
        
        ProfileChangedEventArgs? eventArgs = null;
        manager.ProfileChanged += (sender, args) => eventArgs = args;

        // Act
        await manager.ActivateProfile("unity");

        // Assert
        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.PreviousProfile);
        Assert.Equal("unity", eventArgs.CurrentProfile?.ProfileId);
    }

    [Fact]
    public async Task GetSupportedProfiles_ReturnsOrderedByPriority()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();

        // Act
        var supportedProfiles = await manager.GetSupportedProfiles();
        var profileList = supportedProfiles.ToList();

        // Assert
        Assert.NotEmpty(profileList);
        
        // TUI should be first (highest priority = 100)
        Assert.Equal("tui", profileList[0].ProfileId);
        
        // Unity should be second (priority = 50)
        Assert.Equal("unity", profileList[1].ProfileId);
        
        // Godot should be third (priority = 40)
        Assert.Equal("godot", profileList[2].ProfileId);
    }

    [Fact]
    public async Task RegisterProfile_CustomProfile_AddsToAvailable()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();
        var customProfile = new MockProfile("custom", "Custom Profile", 200);

        // Act
        manager.RegisterProfile(customProfile);
        var availableProfiles = await manager.GetAvailableProfiles();

        // Assert
        Assert.Contains(customProfile, availableProfiles);
    }

    [Fact]
    public async Task ActivateBestProfile_CustomHighPriorityProfile_ActivatesCustom()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();
        var customProfile = new MockProfile("custom", "Custom Profile", 200); // Higher than TUI's 100
        manager.RegisterProfile(customProfile);

        // Act
        var result = await manager.ActivateBestProfile();

        // Assert
        Assert.NotNull(result);
        Assert.Equal("custom", result.ProfileId);
        Assert.Equal(customProfile, manager.ActiveProfile);
    }

    [Fact]
    public async Task DeactivateCurrentProfile_WithActiveProfile_SetsToNull()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();
        await manager.ActivateProfile("tui");
        
        ProfileChangedEventArgs? eventArgs = null;
        manager.ProfileChanged += (sender, args) => eventArgs = args;

        // Act
        await manager.DeactivateCurrentProfile();

        // Assert
        Assert.Null(manager.ActiveProfile);
        Assert.NotNull(eventArgs);
        Assert.Equal("tui", eventArgs.PreviousProfile?.ProfileId);
        Assert.Null(eventArgs.CurrentProfile);
    }

    [Fact]
    public async Task StopAsync_DeactivatesProfileAndStopsService()
    {
        // Arrange
        var manager = new ProfileManager(_mockServiceProvider, _mockLogger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act
        await manager.StopAsync();

        // Assert
        Assert.Null(manager.ActiveProfile);
        Assert.False(manager.IsRunning);
    }
}

/// <summary>
/// Mock profile for testing purposes.
/// </summary>
public class MockProfile : IProfileConfiguration
{
    public string ProfileId { get; }
    public string DisplayName { get; }
    public string Description => "Mock profile for testing";
    public int Priority { get; }

    public MockProfile(string profileId, string displayName, int priority)
    {
        ProfileId = profileId;
        DisplayName = displayName;
        Priority = priority;
    }

    public Task<bool> IsSupported(CancellationToken cancellationToken = default) => Task.FromResult(true);

    public Task<IEnumerable<Type>> GetCapabilityProviders(CancellationToken cancellationToken = default) =>
        Task.FromResult<IEnumerable<Type>>(new List<Type>());

    public Task<IReadOnlyDictionary<string, object>> GetConfigurationSettings(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyDictionary<string, object>>(new Dictionary<string, object>());
}

/// <summary>
/// Mock service provider for testing.
/// </summary>
public class MockServiceProvider : IServiceProvider
{
    public object? GetService(Type serviceType) => null;
}

/// <summary>
/// Mock logger for testing.
/// </summary>
public class MockLogger<T> : ILogger<T>
{
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => true;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}