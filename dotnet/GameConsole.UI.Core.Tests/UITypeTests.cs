using FluentAssertions;
using GameConsole.UI.Core;
using System.Reactive.Linq;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UITypeTests
{
    [Fact]
    public void UIFrameworkType_Should_Have_Expected_Values()
    {
        // Arrange & Act & Assert
        Enum.GetValues<UIFrameworkType>().Should().Contain(new[]
        {
            UIFrameworkType.Console,
            UIFrameworkType.Web,
            UIFrameworkType.Desktop
        });
    }

    [Fact]
    public void UICapabilities_Should_Support_Flag_Operations()
    {
        // Arrange
        var capabilities = UICapabilities.TextDisplay | UICapabilities.ColorSupport | UICapabilities.MouseInteraction;

        // Act & Assert
        capabilities.HasFlag(UICapabilities.TextDisplay).Should().BeTrue();
        capabilities.HasFlag(UICapabilities.ColorSupport).Should().BeTrue();
        capabilities.HasFlag(UICapabilities.MouseInteraction).Should().BeTrue();
        capabilities.HasFlag(UICapabilities.Graphics3D).Should().BeFalse();
    }

    [Fact]
    public void UIStyle_Should_Support_Immutable_Records()
    {
        // Arrange
        var style1 = new UIStyle
        {
            BackgroundColor = 0xFF0000FF,
            FontSize = 14.0f,
            IsBold = true
        };

        var style2 = style1 with { FontSize = 16.0f };

        // Act & Assert
        style1.FontSize.Should().Be(14.0f);
        style2.FontSize.Should().Be(16.0f);
        style2.BackgroundColor.Should().Be(style1.BackgroundColor);
        style2.IsBold.Should().Be(style1.IsBold);
    }

    [Fact]
    public void UISpacing_Should_Create_Uniform_Spacing()
    {
        // Arrange & Act
        var uniform = UISpacing.All(10);
        var symmetric = UISpacing.Symmetric(20, 30);

        // Assert
        uniform.Top.Should().Be(10);
        uniform.Right.Should().Be(10);
        uniform.Bottom.Should().Be(10);
        uniform.Left.Should().Be(10);

        symmetric.Top.Should().Be(30);
        symmetric.Right.Should().Be(20);
        symmetric.Bottom.Should().Be(30);
        symmetric.Left.Should().Be(20);
    }

    [Fact]
    public void UIBreakpoint_Should_Have_Predefined_Values()
    {
        // Act & Assert
        UIBreakpoint.Mobile.Name.Should().Be("Mobile");
        UIBreakpoint.Mobile.MinWidth.Should().Be(0);
        UIBreakpoint.Mobile.MaxWidth.Should().Be(767);

        UIBreakpoint.Tablet.Name.Should().Be("Tablet");
        UIBreakpoint.Tablet.MinWidth.Should().Be(768);
        UIBreakpoint.Tablet.MaxWidth.Should().Be(1023);

        UIBreakpoint.Desktop.Name.Should().Be("Desktop");
        UIBreakpoint.Desktop.MinWidth.Should().Be(1024);
        UIBreakpoint.Desktop.MaxWidth.Should().Be(float.MaxValue);
    }

    [Fact]
    public void UIContext_Should_Require_Essential_Properties()
    {
        // Arrange
        var viewport = new UILayout { Width = 800, Height = 600 };

        // Act
        var context = new UIContext
        {
            Framework = UIFrameworkType.Web,
            SupportedCapabilities = UICapabilities.TextDisplay | UICapabilities.ColorSupport,
            Viewport = viewport
        };

        // Assert
        context.Framework.Should().Be(UIFrameworkType.Web);
        context.SupportedCapabilities.Should().HaveFlag(UICapabilities.TextDisplay);
        context.Viewport.Should().Be(viewport);
    }
}