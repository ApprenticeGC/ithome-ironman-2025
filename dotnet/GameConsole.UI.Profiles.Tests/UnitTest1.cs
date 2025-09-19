using GameConsole.UI.Profiles;
using GameConsole.UI.Profiles.BuiltIn;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace GameConsole.UI.Profiles.Tests;

public class UIProfileSystemTests
{
    private readonly ILogger<UIProfileManager> _logger;

    public UIProfileSystemTests()
    {
        _logger = NullLogger<UIProfileManager>.Instance;
    }

    [Fact]
    public async Task UIProfileManager_InitializeAsync_RegistersBuiltInProfiles()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);

        // Act
        await manager.InitializeAsync();

        // Assert
        Assert.True(manager.Profiles.Count >= 5); // At least Console, Game, Editor, Web, Desktop
        Assert.Contains("Console", manager.Profiles.Keys);
        Assert.Contains("Game", manager.Profiles.Keys);
        Assert.Contains("Editor", manager.Profiles.Keys);
        Assert.Contains("Web", manager.Profiles.Keys);
        Assert.Contains("Desktop", manager.Profiles.Keys);
    }

    [Fact]
    public async Task UIProfileManager_SwitchProfileAsync_ChangesActiveProfile()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        // Act
        await manager.SwitchProfileAsync("Console");

        // Assert
        Assert.NotNull(manager.ActiveProfile);
        Assert.Equal("Console", manager.ActiveProfile.Name);
        Assert.Equal(ConsoleMode.Console, manager.ActiveProfile.TargetMode);

        // Cleanup
        await manager.StopAsync();
    }

    [Fact]
    public void ConsoleProfile_Validate_ReturnsValidResult()
    {
        // Arrange
        var profile = new ConsoleProfile();

        // Act
        var result = profile.Validate();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void GameProfile_GetCommandSet_ContainsGameCommands()
    {
        // Arrange
        var profile = new GameProfile();

        // Act
        var commandSet = profile.GetCommandSet();

        // Assert
        Assert.True(commandSet.HasCommand("play"));
        Assert.True(commandSet.HasCommand("pause"));
        Assert.True(commandSet.HasCommand("stop"));
        Assert.True(commandSet.HasCommand("debug"));
    }

    [Fact]
    public void EditorProfile_GetLayoutConfiguration_HasRequiredPanels()
    {
        // Arrange
        var profile = new EditorProfile();

        // Act
        var layout = profile.GetLayoutConfiguration();

        // Assert
        Assert.True(layout.IsValid());
        Assert.Contains(layout.Panels, p => p.Name == "SceneView");
        Assert.Contains(layout.Panels, p => p.Name == "Hierarchy");
        Assert.Contains(layout.Panels, p => p.Name == "Inspector");
        Assert.Contains(layout.Panels, p => p.Name == "Project");
    }

    [Fact]
    public void ProfileValidator_ValidateProfile_IdentifiesIssues()
    {
        // Arrange
        var validator = new ProfileValidator(NullLogger<ProfileValidator>.Instance);
        var profile = new ConsoleProfile();

        // Act
        var result = validator.ValidateProfile(profile);

        // Assert
        Assert.True(result.IsValid); // Console profile should be valid
    }

    [Fact]
    public async Task ProfileSwitcher_SwitchProfileAsync_CompletesSuccessfully()
    {
        // Arrange
        var manager = new UIProfileManager(_logger);
        await manager.InitializeAsync();
        await manager.StartAsync();

        var switcher = new ProfileSwitcher(manager, NullLogger<ProfileSwitcher>.Instance);

        // Act
        var result = await switcher.SwitchProfileAsync("Console");

        // Assert
        Assert.True(result.Success);
        Assert.Equal("Console", manager.ActiveProfile?.Name);

        // Cleanup
        await manager.StopAsync();
    }

    [Fact]
    public void ProfileValidator_ValidateProfiles_ValidatesBuiltInProfiles()
    {
        // Arrange
        var validator = new ProfileValidator(NullLogger<ProfileValidator>.Instance);
        var profiles = new IUIProfile[]
        {
            new ConsoleProfile(),
            new GameProfile(),
            new EditorProfile()
        };

        // Act
        var results = validator.ValidateProfiles(profiles);

        // Assert
        Assert.Equal(3, results.Count);
        
        // All built-in profiles should be valid
        foreach (var (profileName, result) in results)
        {
            Assert.True(result.IsValid, $"Profile {profileName} should be valid but has errors: [{string.Join(", ", result.Errors)}]");
        }
    }

    [Fact]
    public void UIProfile_CreateVariant_CreatesModifiedProfile()
    {
        // Arrange
        var baseProfile = new ConsoleProfile();
        var modifications = new ProfileModifications
        {
            CommandChanges = new Dictionary<string, CommandDefinition>
            {
                ["test"] = new CommandDefinition { Category = "Test", Description = "Test command" }
            }
        };

        // Act
        var variant = baseProfile.CreateVariant("TestConsole", modifications);

        // Assert
        Assert.Equal("TestConsole", variant.Name);
        Assert.Equal(ConsoleMode.Console, variant.TargetMode);
        Assert.True(variant.GetCommandSet().HasCommand("test"));
    }
}