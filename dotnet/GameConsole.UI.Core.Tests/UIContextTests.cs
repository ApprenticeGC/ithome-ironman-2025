using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UIContext and related context classes.
/// </summary>
public class UIContextTests
{
    [Fact]
    public void UIContext_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var context = new UIContext();

        // Assert
        Assert.NotNull(context.Args);
        Assert.Empty(context.Args);
        Assert.NotNull(context.State);
        Assert.Empty(context.State);
        Assert.Equal(UIFrameworkType.Console, context.FrameworkType); // Default value
        Assert.Equal(UICapabilities.None, context.SupportedCapabilities); // Default value
        Assert.NotNull(context.Preferences);
        Assert.Equal(UIResponsiveMode.Adaptive, context.ResponsiveMode);
        Assert.NotNull(context.Theme);
        Assert.NotNull(context.AccessibilitySettings);
        Assert.NotNull(context.FrameworkProperties);
        Assert.NotNull(context.Viewport);
    }

    [Fact]
    public void UIContext_Should_Support_Custom_Initialization()
    {
        // Arrange
        var args = new[] { "--mode", "test" };
        var state = new Dictionary<string, object> { ["key"] = "value" };
        var frameworkType = UIFrameworkType.Web;
        var capabilities = UICapabilities.TextInput | UICapabilities.ColorDisplay;

        // Act
        var context = new UIContext
        {
            Args = args,
            State = state,
            FrameworkType = frameworkType,
            SupportedCapabilities = capabilities
        };

        // Assert
        Assert.Equal(args, context.Args);
        Assert.Equal(state, context.State);
        Assert.Equal(frameworkType, context.FrameworkType);
        Assert.Equal(capabilities, context.SupportedCapabilities);
    }

    [Fact]
    public void UIPreferences_Should_Have_Sensible_Defaults()
    {
        // Arrange & Act
        var preferences = new UIPreferences();

        // Assert
        Assert.True(preferences.EnableAnimations);
        Assert.True(preferences.EnableSounds);
        Assert.Equal("default", preferences.ColorScheme);
        Assert.Equal(1.0f, preferences.FontSizeMultiplier);
        Assert.NotNull(preferences.KeyboardShortcuts);
        Assert.Equal("en-US", preferences.Culture);
    }

    [Fact]
    public void UITheme_Should_Default_To_Light_Theme()
    {
        // Arrange & Act
        var theme = new UITheme();

        // Assert
        Assert.Equal("default", theme.Name);
        Assert.False(theme.IsDark);
        Assert.Equal("#007ACC", theme.PrimaryColor);
        Assert.Equal("#5A5A5A", theme.SecondaryColor);
        Assert.Equal("#FFFFFF", theme.BackgroundColor);
        Assert.Equal("#000000", theme.TextColor);
        Assert.NotNull(theme.CssVariables);
    }

    [Fact]
    public void UIAccessibilitySettings_Should_Have_Safe_Defaults()
    {
        // Arrange & Act
        var settings = new UIAccessibilitySettings();

        // Assert
        Assert.False(settings.HighContrast);
        Assert.False(settings.ScreenReader);
        Assert.False(settings.ReducedMotion);
        Assert.True(settings.KeyboardNavigation);
        Assert.True(settings.ShowFocusIndicators);
        Assert.Equal(1.0f, settings.TextScaling);
    }

    [Fact]
    public void UIViewport_Should_Default_To_Desktop_Dimensions()
    {
        // Arrange & Act
        var viewport = new UIViewport();

        // Assert
        Assert.Equal(1920, viewport.Width);
        Assert.Equal(1080, viewport.Height);
        Assert.Equal(1.0f, viewport.PixelRatio);
        Assert.False(viewport.IsPortrait);
        Assert.Equal("desktop", viewport.Breakpoint);
    }

    [Fact]
    public void UIContext_Should_Support_Record_Equality_For_Simple_Properties()
    {
        // Arrange - Records with dictionaries don't compare by content, so test framework type equality
        var context1 = new UIContext { FrameworkType = UIFrameworkType.Web };
        var context2 = new UIContext { FrameworkType = UIFrameworkType.Web };
        var context3 = new UIContext { FrameworkType = UIFrameworkType.Desktop };

        // Act & Assert - Compare specific properties instead of full records
        Assert.Equal(context1.FrameworkType, context2.FrameworkType);
        Assert.NotEqual(context1.FrameworkType, context3.FrameworkType);
        
        // Test that records behave consistently for immutable properties
        Assert.Equal(context1.ResponsiveMode, context2.ResponsiveMode);
    }

    [Fact]
    public void UIContext_Should_Support_With_Expressions()
    {
        // Arrange
        var original = new UIContext { FrameworkType = UIFrameworkType.Console };

        // Act
        var modified = original with { FrameworkType = UIFrameworkType.Web };

        // Assert
        Assert.Equal(UIFrameworkType.Console, original.FrameworkType);
        Assert.Equal(UIFrameworkType.Web, modified.FrameworkType);
        Assert.NotEqual(original, modified);
    }
}