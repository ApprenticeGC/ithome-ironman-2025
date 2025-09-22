using GameConsole.UI.Core;

namespace GameConsole.UI.Core.Tests;

public class UIProfileTests
{
    [Fact]
    public void UIProfile_Should_Have_Required_Properties()
    {
        // Arrange & Act
        var profile = new UIProfile
        {
            Id = "test-profile",
            Name = "Test Profile",
            Description = "Test Description",
            Mode = "TUI",
            Settings = new UIProfileSettings(),
            IsActive = true,
            IsBuiltIn = false
        };

        // Assert
        Assert.Equal("test-profile", profile.Id);
        Assert.Equal("Test Profile", profile.Name);
        Assert.Equal("Test Description", profile.Description);
        Assert.Equal("TUI", profile.Mode);
        Assert.True(profile.IsActive);
        Assert.False(profile.IsBuiltIn);
        Assert.NotNull(profile.Settings);
        Assert.True(profile.CreatedAt <= DateTimeOffset.UtcNow);
        Assert.True(profile.ModifiedAt <= DateTimeOffset.UtcNow);
    }

    [Fact]
    public void UIProfile_With_Modification_Should_Update_ModifiedAt()
    {
        // Arrange
        var originalProfile = new UIProfile
        {
            Id = "test",
            Name = "Original",
            CreatedAt = DateTimeOffset.UtcNow.AddDays(-1),
            ModifiedAt = DateTimeOffset.UtcNow.AddDays(-1)
        };

        // Act
        var modifiedProfile = originalProfile with
        {
            Name = "Modified",
            ModifiedAt = DateTimeOffset.UtcNow
        };

        // Assert
        Assert.Equal("Original", originalProfile.Name);
        Assert.Equal("Modified", modifiedProfile.Name);
        Assert.Equal(originalProfile.CreatedAt, modifiedProfile.CreatedAt);
        Assert.True(modifiedProfile.ModifiedAt > originalProfile.ModifiedAt);
    }

    [Fact]
    public void UIProfileSettings_Should_Have_Default_Values()
    {
        // Arrange & Act
        var settings = new UIProfileSettings();

        // Assert
        Assert.NotNull(settings.Theme);
        Assert.NotNull(settings.Layout);
        Assert.NotNull(settings.Input);
        Assert.NotNull(settings.Rendering);
        Assert.NotNull(settings.Properties);
        Assert.Empty(settings.Properties);
    }

    [Fact]
    public void UIThemeSettings_Should_Have_Reasonable_Defaults()
    {
        // Arrange & Act
        var theme = new UIThemeSettings();

        // Assert
        Assert.Equal("Default", theme.ColorScheme);
        Assert.Equal("Consolas", theme.FontFamily);
        Assert.Equal(14, theme.FontSize);
        Assert.True(theme.DarkMode);
    }

    [Fact]
    public void UILayoutSettings_Should_Have_Reasonable_Defaults()
    {
        // Arrange & Act
        var layout = new UILayoutSettings();

        // Assert
        Assert.Equal("100%", layout.Width);
        Assert.Equal("100%", layout.Height);
        Assert.Equal(8, layout.Padding);
        Assert.True(layout.ShowBorders);
    }

    [Fact]
    public void UIInputSettings_Should_Have_Reasonable_Defaults()
    {
        // Arrange & Act
        var input = new UIInputSettings();

        // Assert
        Assert.Equal("Keyboard", input.PreferredInput);
        Assert.True(input.KeyboardShortcuts);
        Assert.True(input.MouseEnabled);
    }

    [Fact]
    public void UIRenderingSettings_Should_Have_Reasonable_Defaults()
    {
        // Arrange & Act
        var rendering = new UIRenderingSettings();

        // Assert
        Assert.Equal(60, rendering.MaxFPS);
        Assert.True(rendering.UseHardwareAcceleration);
        Assert.Equal("High", rendering.Quality);
    }
}

public class UIProfileValidationResultTests
{
    [Fact]
    public void Success_Should_Return_Valid_Result()
    {
        // Act
        var result = UIProfileValidationResult.Success();

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void Failed_Should_Return_Invalid_Result_With_Errors()
    {
        // Arrange
        var errors = new[] { "Error 1", "Error 2" };

        // Act
        var result = UIProfileValidationResult.Failed(errors);

        // Assert
        Assert.False(result.IsValid);
        Assert.Equal(errors, result.Errors);
        Assert.Empty(result.Warnings);
    }

    [Fact]
    public void WithWarnings_Should_Return_Valid_Result_With_Warnings()
    {
        // Arrange
        var warnings = new[] { "Warning 1", "Warning 2" };

        // Act
        var result = UIProfileValidationResult.WithWarnings(warnings);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(warnings, result.Warnings);
    }
}

public class UIProfileChangedEventArgsTests
{
    [Fact]
    public void Constructor_Should_Set_Properties()
    {
        // Arrange
        var profile = new UIProfile { Id = "test", Name = "Test" };
        var changeType = UIProfileChangeType.Created;

        // Act
        var eventArgs = new UIProfileChangedEventArgs(profile, changeType);

        // Assert
        Assert.Equal(profile, eventArgs.Profile);
        Assert.Equal(changeType, eventArgs.ChangeType);
        Assert.True(eventArgs.Timestamp <= DateTimeOffset.UtcNow);
    }
}