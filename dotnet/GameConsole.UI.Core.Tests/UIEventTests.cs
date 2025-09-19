using FluentAssertions;
using GameConsole.UI.Core;
using System.Reactive.Linq;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIEventTests
{
    [Fact]
    public void UIEvent_Should_Have_Timestamp()
    {
        // Arrange & Act
        var clickEvent = new UIClickEvent
        {
            ComponentId = "test-button"
        };

        // Assert
        clickEvent.Timestamp.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(1));
        clickEvent.ComponentId.Should().Be("test-button");
    }

    [Fact]
    public void UIValueChangedEvent_Should_Track_Value_Changes()
    {
        // Arrange & Act
        var valueChanged = new UIValueChangedEvent
        {
            ComponentId = "text-input",
            OldValue = "old text",
            NewValue = "new text"
        };

        // Assert
        valueChanged.ComponentId.Should().Be("text-input");
        valueChanged.OldValue.Should().Be("old text");
        valueChanged.NewValue.Should().Be("new text");
    }

    [Fact]
    public void UIClickEvent_Should_Support_Position_And_Button()
    {
        // Arrange & Act
        var clickEvent = new UIClickEvent
        {
            ComponentId = "clickable",
            Position = (100, 200),
            Button = 1 // Right click
        };

        // Assert
        clickEvent.ComponentId.Should().Be("clickable");
        clickEvent.Position.Should().Be((100f, 200f));
        clickEvent.Button.Should().Be(1);
    }

    [Fact]
    public void UIKeyEvent_Should_Support_Key_And_Modifiers()
    {
        // Arrange & Act
        var keyEvent = new UIKeyEvent
        {
            ComponentId = "input-field",
            Key = "Enter",
            IsKeyDown = true,
            Modifiers = UIKeyModifiers.Control | UIKeyModifiers.Shift
        };

        // Assert
        keyEvent.ComponentId.Should().Be("input-field");
        keyEvent.Key.Should().Be("Enter");
        keyEvent.IsKeyDown.Should().BeTrue();
        keyEvent.Modifiers.HasFlag(UIKeyModifiers.Control).Should().BeTrue();
        keyEvent.Modifiers.HasFlag(UIKeyModifiers.Shift).Should().BeTrue();
        keyEvent.Modifiers.HasFlag(UIKeyModifiers.Alt).Should().BeFalse();
    }

    [Fact]
    public void UIFocusEvent_Should_Track_Focus_State()
    {
        // Arrange & Act
        var focusGained = new UIFocusEvent
        {
            ComponentId = "focused-input",
            HasFocus = true
        };

        var focusLost = new UIFocusEvent
        {
            ComponentId = "unfocused-input",
            HasFocus = false
        };

        // Assert
        focusGained.HasFocus.Should().BeTrue();
        focusLost.HasFocus.Should().BeFalse();
    }

    [Fact]
    public void UILifecycleEvent_Should_Track_Component_Lifecycle()
    {
        // Arrange & Act
        var lifecycleEvent = new UILifecycleEvent
        {
            ComponentId = "lifecycle-component",
            Stage = UILifecycleStage.Mounted
        };

        // Assert
        lifecycleEvent.ComponentId.Should().Be("lifecycle-component");
        lifecycleEvent.Stage.Should().Be(UILifecycleStage.Mounted);
    }

    [Fact]
    public void UIDataBindingEvent_Should_Track_Data_Binding()
    {
        // Arrange & Act
        var bindingEvent = new UIDataBindingEvent
        {
            ComponentId = "bound-component",
            PropertyName = "Text",
            Data = "bound value",
            BindingPath = "viewModel.Name"
        };

        // Assert
        bindingEvent.ComponentId.Should().Be("bound-component");
        bindingEvent.PropertyName.Should().Be("Text");
        bindingEvent.Data.Should().Be("bound value");
        bindingEvent.BindingPath.Should().Be("viewModel.Name");
    }
}