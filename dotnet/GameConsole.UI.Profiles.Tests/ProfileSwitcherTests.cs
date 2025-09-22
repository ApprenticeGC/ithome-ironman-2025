using Xunit;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the ProfileSwitcher class, validating runtime mode transitions.
/// These tests ensure smooth transitions with minimal user disruption as specified in RFC-011-01.
/// </summary>
public class ProfileSwitcherTests
{
    /// <summary>
    /// Test successful profile switching through ProfileSwitcher.
    /// Validates: Smooth transitions with minimal user disruption
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Switch_Successfully()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        manager.RegisterProfile(profile);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Track events
        var switchStartedFired = false;
        var switchCompletedFired = false;
        
        switcher.SwitchStarted += (sender, args) => switchStartedFired = true;
        switcher.SwitchCompleted += (sender, args) => switchCompletedFired = true;

        // Act
        var result = await switcher.SwitchToProfileAsync("TestProfile", context, preserveState: true);

        // Assert
        Assert.True(result.Success);
        Assert.Null(result.ErrorMessage);
        Assert.True(switchStartedFired);
        Assert.True(switchCompletedFired);
        Assert.Equal(ProfileSwitchState.Idle, switcher.CurrentState);
    }

    /// <summary>
    /// Test profile switch failure handling.
    /// Validates: ProfileSwitcher handles failures gracefully
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Handle_Failure_Gracefully()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);
        var context = new TestUIContext(ConsoleMode.Game);

        await manager.InitializeAsync();
        await manager.StartAsync();

        // Track events
        var switchFailedFired = false;
        switcher.SwitchFailed += (sender, args) => switchFailedFired = true;

        // Act
        var result = await switcher.SwitchToProfileAsync("NonExistentProfile", context);

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.ErrorMessage);
        Assert.True(switchFailedFired);
        Assert.Equal(ProfileSwitchState.Idle, switcher.CurrentState);
    }

    /// <summary>
    /// Test that state machine prevents concurrent switches.
    /// Validates: State machine pattern for profile transitions
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Prevent_Concurrent_Switches()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);
        var profile1 = new SlowActivationTestUIProfile("Profile1", ConsoleMode.Game);
        var profile2 = new TestUIProfile("Profile2", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        manager.RegisterProfile(profile1);
        manager.RegisterProfile(profile2);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act - Start first switch (will take some time) and immediately try second switch
        var firstSwitchTask = switcher.SwitchToProfileAsync("Profile1", context);
        
        // Small delay to ensure first switch has started
        await Task.Delay(10);
        
        // Check state is switching
        Assert.Equal(ProfileSwitchState.Switching, switcher.CurrentState);
        
        // Try to start second switch while first is in progress
        var secondResult = await switcher.SwitchToProfileAsync("Profile2", context);

        // Wait for first switch to complete
        var firstResult = await firstSwitchTask;

        // Assert
        Assert.True(firstResult.Success); // First switch should succeed
        Assert.False(secondResult.Success); // Second should fail due to concurrent switch
        Assert.Contains("Profile switch already in progress", secondResult.ErrorMessage);
        Assert.Equal(ProfileSwitchState.Idle, switcher.CurrentState); // Should be idle after completion
    }

    /// <summary>
    /// Test profile switch event args contain correct information.
    /// Validates: Event system provides complete switch information
    /// </summary>
    [Fact]
    public async Task SwitchToProfileAsync_Should_Provide_Correct_Event_Information()
    {
        // Arrange
        var validator = new ProfileValidator();
        var manager = new UIProfileManager(validator);
        var switcher = new ProfileSwitcher(manager);
        var profile = new TestUIProfile("TestProfile", ConsoleMode.Game);
        var context = new TestUIContext(ConsoleMode.Game);

        manager.RegisterProfile(profile);
        await manager.InitializeAsync();
        await manager.StartAsync();

        ProfileSwitchEventArgs? capturedArgs = null;
        switcher.SwitchStarted += (sender, args) => capturedArgs = args;

        // Act
        await switcher.SwitchToProfileAsync("TestProfile", context, preserveState: true);

        // Assert
        Assert.NotNull(capturedArgs);
        Assert.Equal(string.Empty, capturedArgs.FromProfile); // No active profile initially
        Assert.Equal("TestProfile", capturedArgs.ToProfile);
        Assert.True(capturedArgs.PreserveState);
    }
}