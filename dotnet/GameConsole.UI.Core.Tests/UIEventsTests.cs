using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI event types.
/// </summary>
public class UIEventsTests
{
    [Fact]
    public void UIEvent_Should_Initialize_With_Timestamp_And_Id()
    {
        // Arrange
        var timestamp = DateTimeOffset.UtcNow;

        // Act
        var evt = new TestUIEvent();

        // Assert
        Assert.True(evt.Timestamp >= timestamp);
        Assert.False(string.IsNullOrEmpty(evt.EventId));
        Assert.True(evt.Bubbles);
        Assert.False(evt.Cancelable);
        Assert.False(evt.Cancelled);
    }

    [Fact]
    public void UIRenderEvent_Should_Inherit_From_UIEvent()
    {
        // Arrange & Act
        var renderEvent = new UIRenderEvent
        {
            ComponentId = "test-component",
            RenderDuration = 16.7, // ~60 FPS
            IsInitialRender = true
        };

        // Assert
        Assert.IsAssignableFrom<UIEvent>(renderEvent);
        Assert.Equal("test-component", renderEvent.ComponentId);
        Assert.Equal(16.7, renderEvent.RenderDuration);
        Assert.True(renderEvent.IsInitialRender);
    }

    [Fact]
    public void UIInteractionEvent_Should_Support_Different_Interaction_Types()
    {
        // Arrange
        var clickEvent = new UIInteractionEvent
        {
            InteractionType = UIInteractionType.Click,
            ComponentId = "button-1",
            Data = new Dictionary<string, object> { ["button"] = "left" }
        };

        var keyEvent = new UIInteractionEvent
        {
            InteractionType = UIInteractionType.KeyPress,
            ComponentId = "input-1",
            Data = new Dictionary<string, object> { ["key"] = "Enter" }
        };

        // Act & Assert
        Assert.Equal(UIInteractionType.Click, clickEvent.InteractionType);
        Assert.Equal("button-1", clickEvent.ComponentId);
        Assert.Equal("left", clickEvent.Data["button"]);

        Assert.Equal(UIInteractionType.KeyPress, keyEvent.InteractionType);
        Assert.Equal("input-1", keyEvent.ComponentId);
        Assert.Equal("Enter", keyEvent.Data["key"]);
    }

    [Fact]
    public void UIStateChangeEvent_Should_Track_Property_Changes()
    {
        // Arrange & Act
        var stateChangeEvent = new UIStateChangeEvent
        {
            ComponentId = "label-1",
            PropertyName = "Text",
            OldValue = "Old Text",
            NewValue = "New Text"
        };

        // Assert
        Assert.Equal("label-1", stateChangeEvent.ComponentId);
        Assert.Equal("Text", stateChangeEvent.PropertyName);
        Assert.Equal("Old Text", stateChangeEvent.OldValue);
        Assert.Equal("New Text", stateChangeEvent.NewValue);
    }

    [Fact]
    public void UIFrameworkEvent_Should_Support_Framework_Changes()
    {
        // Arrange & Act
        var frameworkEvent = new UIFrameworkEvent
        {
            EventType = UIFrameworkEventType.SwitchingFramework,
            OldFrameworkType = UIFrameworkType.Console,
            NewFrameworkType = UIFrameworkType.Web,
            EventData = new Dictionary<string, object> { ["reason"] = "user_preference" }
        };

        // Assert
        Assert.Equal(UIFrameworkEventType.SwitchingFramework, frameworkEvent.EventType);
        Assert.Equal(UIFrameworkType.Console, frameworkEvent.OldFrameworkType);
        Assert.Equal(UIFrameworkType.Web, frameworkEvent.NewFrameworkType);
        Assert.Equal("user_preference", frameworkEvent.EventData["reason"]);
    }

    [Fact]
    public void UIErrorEvent_Should_Capture_Exception_Details()
    {
        // Arrange
        var exception = new InvalidOperationException("Test error message");

        // Act
        var errorEvent = new UIErrorEvent
        {
            Error = exception,
            ComponentId = "failing-component",
            Severity = UIErrorSeverity.Error
        };

        // Assert
        Assert.Equal(exception, errorEvent.Error);
        Assert.Equal("failing-component", errorEvent.ComponentId);
        Assert.Equal(UIErrorSeverity.Error, errorEvent.Severity);
        Assert.False(errorEvent.Handled);
    }

    [Fact]
    public void UIInteractionType_Should_Cover_Common_Interactions()
    {
        // Arrange & Act
        var values = Enum.GetValues<UIInteractionType>();

        // Assert - Check for common interaction types
        Assert.Contains(UIInteractionType.Click, values);
        Assert.Contains(UIInteractionType.DoubleClick, values);
        Assert.Contains(UIInteractionType.Hover, values);
        Assert.Contains(UIInteractionType.Focus, values);
        Assert.Contains(UIInteractionType.Blur, values);
        Assert.Contains(UIInteractionType.KeyPress, values);
        Assert.Contains(UIInteractionType.Input, values);
        Assert.Contains(UIInteractionType.Change, values);
        Assert.Contains(UIInteractionType.Submit, values);
        Assert.Contains(UIInteractionType.DragStart, values);
        Assert.Contains(UIInteractionType.DragEnd, values);
        Assert.Contains(UIInteractionType.Drop, values);
    }

    [Fact]
    public void UIFrameworkEventType_Should_Cover_Lifecycle_Events()
    {
        // Arrange & Act
        var values = Enum.GetValues<UIFrameworkEventType>();

        // Assert - Check for framework lifecycle events
        Assert.Contains(UIFrameworkEventType.Initializing, values);
        Assert.Contains(UIFrameworkEventType.Initialized, values);
        Assert.Contains(UIFrameworkEventType.Starting, values);
        Assert.Contains(UIFrameworkEventType.Started, values);
        Assert.Contains(UIFrameworkEventType.Stopping, values);
        Assert.Contains(UIFrameworkEventType.Stopped, values);
        Assert.Contains(UIFrameworkEventType.SwitchingFramework, values);
        Assert.Contains(UIFrameworkEventType.ThemeChanged, values);
        Assert.Contains(UIFrameworkEventType.ViewportChanged, values);
        Assert.Contains(UIFrameworkEventType.ConfigurationUpdated, values);
    }

    [Theory]
    [InlineData(UIErrorSeverity.Info)]
    [InlineData(UIErrorSeverity.Warning)]
    [InlineData(UIErrorSeverity.Error)]
    [InlineData(UIErrorSeverity.Critical)]
    public void UIErrorSeverity_Should_Have_All_Levels(UIErrorSeverity severity)
    {
        // Arrange & Act
        var values = Enum.GetValues<UIErrorSeverity>();

        // Assert
        Assert.Contains(severity, values);
    }

    // Test helper class
    private record TestUIEvent : UIEvent;
}