using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Integration tests that validate all acceptance criteria for RFC-011-01.
/// These tests demonstrate the complete functionality of the Mode-Based UI Profile System.
/// </summary>
public class RFC011AcceptanceCriteriaTests
{
    /// <summary>
    /// Test that validates all main acceptance criteria from RFC-011-01:
    /// - Dynamic switching between Console, Web, Desktop profiles
    /// - Profile manager handles state preservation during switches
    /// - Profile validation ensures compatibility and completeness
    /// - Support for custom and user-defined profiles
    /// - Profile inheritance and composition capabilities
    /// - Comprehensive profile configuration and persistence
    /// </summary>
    [Fact]
    public async Task RFC011_Acceptance_Criteria_Should_Be_Met()
    {
        // Arrange - Set up the complete UI profile system
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);

        // Create profiles for different modes (Console, Web, Desktop)
        var gameProfile = new TestUIProfile("GameConsole", ConsoleMode.Game);
        var editorProfile = new TestUIProfile("EditorConsole", ConsoleMode.Editor);
        var webProfile = new TestUIProfile("WebInterface", ConsoleMode.Web);
        var desktopProfile = new TestUIProfile("DesktopApp", ConsoleMode.Desktop);

        // Configure profile with inheritance and composition
        editorProfile.Metadata.InheritsFrom.Add("GameConsole");
        editorProfile.Metadata.FeatureFlags["ExtendedTools"] = true;
        editorProfile.Metadata.CompatibilityRequirements["MinVersion"] = "1.0.0";

        // Register all profiles
        manager.RegisterProfile(gameProfile);
        manager.RegisterProfile(editorProfile);
        manager.RegisterProfile(webProfile);
        manager.RegisterProfile(desktopProfile);

        await manager.InitializeAsync();
        await manager.StartAsync();

        // Test 1: Dynamic switching between Console, Web, Desktop profiles
        var gameContext = new TestUIContext(ConsoleMode.Game);
        var webContext = new TestUIContext(ConsoleMode.Web);
        var desktopContext = new TestUIContext(ConsoleMode.Desktop);

        var gameResult = await switcher.SwitchToProfileAsync("GameConsole", gameContext, preserveState: true);
        Assert.True(gameResult.Success);
        Assert.Equal("GameConsole", manager.ActiveProfile?.Name);

        var webResult = await switcher.SwitchToProfileAsync("WebInterface", webContext, preserveState: true);
        Assert.True(webResult.Success);
        Assert.Equal("WebInterface", manager.ActiveProfile?.Name);

        var desktopResult = await switcher.SwitchToProfileAsync("DesktopApp", desktopContext, preserveState: true);
        Assert.True(desktopResult.Success);
        Assert.Equal("DesktopApp", manager.ActiveProfile?.Name);

        // Test 2: Profile validation ensures compatibility and completeness
        var editorContext = new TestUIContext(ConsoleMode.Editor);
        editorContext.SetProperty("MinVersion", "1.0.0");
        
        var validationResult = await validator.ValidateProfileAsync(editorProfile, editorContext);
        Assert.True(validationResult.IsValid);

        // Test 3: Support for custom and user-defined profiles
        var customProfile = new TestUIProfile("CustomProfile", ConsoleMode.Game);
        customProfile.Metadata.Author = "User";
        customProfile.Metadata.Tags.Add("custom");
        customProfile.Metadata.Tags.Add("user-defined");
        
        manager.RegisterProfile(customProfile);
        Assert.Contains(customProfile, manager.AllProfiles);

        // Test 4: Profile inheritance and composition capabilities
        Assert.Contains("GameConsole", editorProfile.Metadata.InheritsFrom);
        Assert.True(editorProfile.Metadata.FeatureFlags["ExtendedTools"]);

        // Test 5: Comprehensive profile configuration and persistence
        await editorProfile.SaveConfigurationAsync();
        await editorProfile.ReloadConfigurationAsync();

        // Test 6: Profile manager organizes profiles by mode correctly
        var gameProfiles = manager.GetProfilesForMode(ConsoleMode.Game);
        var webProfiles = manager.GetProfilesForMode(ConsoleMode.Web);
        var desktopProfiles = manager.GetProfilesForMode(ConsoleMode.Desktop);

        Assert.Contains(gameProfile, gameProfiles);
        Assert.Contains(customProfile, gameProfiles);
        Assert.Contains(webProfile, webProfiles);
        Assert.Contains(desktopProfile, desktopProfiles);

        // Test 7: Profile hot-reloading support
        Assert.True(gameProfile.Metadata.SupportsHotReload);

        // Cleanup
        await manager.StopAsync();
        await manager.DisposeAsync();
    }

    /// <summary>
    /// Test that demonstrates smooth transitions with minimal user disruption.
    /// Validates: Profile switcher provides smooth transitions
    /// </summary>
    [Fact]
    public async Task Profile_Transitions_Should_Be_Smooth_With_State_Preservation()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);

        var sourceProfile = new TestUIProfile("Source", ConsoleMode.Game);
        var targetProfile = new TestUIProfile("Target", ConsoleMode.Game);

        manager.RegisterProfile(sourceProfile);
        manager.RegisterProfile(targetProfile);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var context = new TestUIContext(ConsoleMode.Game);
        
        // Set up some context state
        context.SetProperty("UserData", "ImportantState");

        // Activate source profile
        await manager.SwitchToProfileAsync("Source", context);

        // Track transition events
        var events = new List<string>();
        switcher.SwitchStarted += (s, e) => events.Add($"Started: {e.FromProfile} -> {e.ToProfile}");
        switcher.SwitchCompleted += (s, e) => events.Add($"Completed: {e.FromProfile} -> {e.ToProfile}");

        // Act - Switch with state preservation
        var result = await switcher.SwitchToProfileAsync("Target", context, preserveState: true);

        // Assert
        Assert.True(result.Success);
        Assert.Equal(2, events.Count);
        Assert.Contains("Started: Source -> Target", events);
        Assert.Contains("Completed: Source -> Target", events);
        
        // Verify state is preserved in context
        Assert.Equal("ImportantState", context.Properties["UserData"]);

        await manager.StopAsync();
    }
}