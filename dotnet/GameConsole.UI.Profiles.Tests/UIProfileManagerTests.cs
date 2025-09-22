using FluentAssertions;
using GameConsole.UI.Profiles.Implementations;

namespace GameConsole.UI.Profiles.Tests;

/// <summary>
/// Tests for the UIProfileManager implementation.
/// </summary>
public class UIProfileManagerTests
{
    [Fact]
    public void Constructor_RegistersBuiltInProfiles()
    {
        // Act
        var manager = new UIProfileManager();
        
        // Assert
        manager.AvailableProfiles.Should().NotBeEmpty();
        manager.AvailableProfiles.Should().HaveCount(2); // Game and Editor profiles
        
        var gameProfile = manager.AvailableProfiles.FirstOrDefault(p => p.TargetMode == ConsoleMode.Game);
        gameProfile.Should().NotBeNull();
        gameProfile.Should().BeOfType<GameModeProfile>();
        
        var editorProfile = manager.AvailableProfiles.FirstOrDefault(p => p.TargetMode == ConsoleMode.Editor);
        editorProfile.Should().NotBeNull();
        editorProfile.Should().BeOfType<EditorModeProfile>();
    }
    
    [Fact]
    public void RegisterProfile_AddsNewProfile()
    {
        // Arrange
        var manager = new UIProfileManager();
        var customProfile = new TestUIProfile("CustomProfile", ConsoleMode.Game);
        
        // Act
        manager.RegisterProfile(customProfile);
        
        // Assert
        manager.AvailableProfiles.Should().Contain(customProfile);
    }
    
    [Fact]
    public void RegisterProfile_ThrowsForDuplicateName()
    {
        // Arrange
        var manager = new UIProfileManager();
        var profile1 = new TestUIProfile("TestProfile", ConsoleMode.Game);
        var profile2 = new TestUIProfile("TestProfile", ConsoleMode.Editor); // Same name
        
        manager.RegisterProfile(profile1);
        
        // Act & Assert
        var action = () => manager.RegisterProfile(profile2);
        action.Should().Throw<InvalidOperationException>()
            .WithMessage("A profile with name 'TestProfile' is already registered");
    }
    
    [Fact]
    public void GetBestMatchingProfile_ReturnsCorrectProfile()
    {
        // Arrange
        var manager = new UIProfileManager();
        var gameContext = new UIProfileContext { Mode = ConsoleMode.Game };
        var editorContext = new UIProfileContext { Mode = ConsoleMode.Editor };
        
        // Act
        var gameProfile = manager.GetBestMatchingProfile(gameContext);
        var editorProfile = manager.GetBestMatchingProfile(editorContext);
        
        // Assert
        gameProfile.Should().NotBeNull();
        gameProfile!.TargetMode.Should().Be(ConsoleMode.Game);
        
        editorProfile.Should().NotBeNull();
        editorProfile!.TargetMode.Should().Be(ConsoleMode.Editor);
    }
    
    [Fact]
    public async Task ActivateProfileAsync_ActivatesCorrectProfile()
    {
        // Arrange
        var manager = new UIProfileManager();
        var context = new UIProfileContext { Mode = ConsoleMode.Game };
        
        // Act
        await manager.ActivateProfileAsync(context);
        
        // Assert
        manager.ActiveProfile.Should().NotBeNull();
        manager.ActiveProfile!.TargetMode.Should().Be(ConsoleMode.Game);
    }
    
    [Fact]
    public async Task ActivateProfileAsync_FiresProfileChangedEvent()
    {
        // Arrange
        var manager = new UIProfileManager();
        var context = new UIProfileContext { Mode = ConsoleMode.Game };
        
        ProfileChangedEventArgs? eventArgs = null;
        manager.ProfileChanged += (sender, args) => eventArgs = args;
        
        // Act
        await manager.ActivateProfileAsync(context);
        
        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.NewProfile.Should().Be(manager.ActiveProfile);
        eventArgs.PreviousProfile.Should().BeNull();
        eventArgs.Context.Should().Be(context);
    }
    
    [Fact]
    public async Task ActivateProfileAsync_SwitchesProfiles()
    {
        // Arrange
        var manager = new UIProfileManager();
        var gameContext = new UIProfileContext { Mode = ConsoleMode.Game };
        var editorContext = new UIProfileContext { Mode = ConsoleMode.Editor };
        
        await manager.ActivateProfileAsync(gameContext);
        var firstProfile = manager.ActiveProfile;
        
        // Act
        await manager.ActivateProfileAsync(editorContext);
        
        // Assert
        manager.ActiveProfile.Should().NotBe(firstProfile);
        manager.ActiveProfile!.TargetMode.Should().Be(ConsoleMode.Editor);
    }
    
    [Fact]
    public async Task ActivateProfileAsync_DoesNotSwitchIfSameProfile()
    {
        // Arrange
        var manager = new UIProfileManager();
        var context = new UIProfileContext { Mode = ConsoleMode.Game };
        
        await manager.ActivateProfileAsync(context);
        var originalProfile = manager.ActiveProfile;
        
        var eventFired = false;
        manager.ProfileChanged += (sender, args) => eventFired = true;
        
        // Act
        await manager.ActivateProfileAsync(context); // Same context again
        
        // Assert
        manager.ActiveProfile.Should().BeSameAs(originalProfile);
        eventFired.Should().BeFalse(); // No event should fire for same profile
    }
    
    [Fact]
    public async Task StartAsync_SetsIsRunningTrue()
    {
        // Arrange
        var manager = new UIProfileManager();
        
        // Act
        await manager.StartAsync();
        
        // Assert
        manager.IsRunning.Should().BeTrue();
    }
    
    [Fact]
    public async Task StopAsync_SetsIsRunningFalse()
    {
        // Arrange
        var manager = new UIProfileManager();
        await manager.StartAsync();
        
        // Act
        await manager.StopAsync();
        
        // Assert
        manager.IsRunning.Should().BeFalse();
    }
}

/// <summary>
/// Test implementation of UIProfile for testing purposes.
/// </summary>
internal class TestUIProfile : UIProfile
{
    public TestUIProfile(string name, ConsoleMode mode)
    {
        Name = name;
        TargetMode = mode;
        Metadata = new UIProfileMetadata
        {
            DisplayName = name,
            Description = "Test profile",
            Version = "1.0.0"
        };
    }
    
    public override CommandSet GetCommandSet() => new();
    public override LayoutConfiguration GetLayoutConfiguration() => new();
    public override KeyBindingSet GetKeyBindings() => new();
    public override MenuConfiguration GetMenuConfiguration() => new();
    public override StatusBarConfiguration GetStatusBarConfiguration() => new();
    public override ToolbarConfiguration GetToolbarConfiguration() => new();
}