using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

public class UIEventsTests
{
    [Fact]
    public void UIEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var data = new Dictionary<string, object> { ["key"] = "value" };
        
        // Act
        var evt = new TestUIEvent("test", timestamp, "comp-1", data);
        
        // Assert
        Assert.Equal("test", evt.EventType);
        Assert.Equal(timestamp, evt.Timestamp);
        Assert.Equal("comp-1", evt.ComponentId);
        Assert.Single(evt.Data!);
        Assert.Equal("value", evt.GetData<string>("key"));
    }

    [Fact]
    public void UIEvent_GetData_ShouldReturnDefaultWhenKeyNotFound()
    {
        // Arrange
        var evt = new TestUIEvent("test", DateTime.UtcNow);
        
        // Act
        var result = evt.GetData<string>("nonexistent", "default");
        
        // Assert
        Assert.Equal("default", result);
    }

    [Fact]
    public void UIEvent_WithData_ShouldCreateNewEventWithData()
    {
        // Arrange
        var original = new TestUIEvent("test", DateTime.UtcNow);
        
        // Act
        var updated = original.WithData("key", "value");
        
        // Assert
        Assert.Null(original.Data);
        Assert.Single(updated.Data!);
        Assert.Equal("value", updated.GetData<string>("key"));
    }

    [Fact]
    public void UIInteractionEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        
        // Act
        var evt = new UIInteractionEvent("btn-1", "click", timestamp, "buttonData");
        
        // Assert
        Assert.Equal("interaction", evt.EventType);
        Assert.Equal("btn-1", evt.ComponentId);
        Assert.Equal("click", evt.ActionType);
        Assert.Equal(timestamp, evt.Timestamp);
        Assert.Equal("buttonData", evt.ActionData);
    }

    [Fact]
    public void UIDataBindingEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        
        // Act
        var evt = new UIDataBindingEvent("input-1", "Value", "oldValue", "newValue", timestamp);
        
        // Assert
        Assert.Equal("dataBinding", evt.EventType);
        Assert.Equal("input-1", evt.ComponentId);
        Assert.Equal("Value", evt.PropertyName);
        Assert.Equal("oldValue", evt.OldValue);
        Assert.Equal("newValue", evt.NewValue);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    [Fact]
    public void UIFocusEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        
        // Act
        var evt = new UIFocusEvent("prev-1", "curr-1", timestamp);
        
        // Assert
        Assert.Equal("focus", evt.EventType);
        Assert.Equal("prev-1", evt.PreviousComponentId);
        Assert.Equal("curr-1", evt.CurrentComponentId);
        Assert.Equal("curr-1", evt.ComponentId);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    [Fact]
    public void UIValidationEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var errors = new[] { "Required field", "Invalid format" };
        
        // Act
        var evt = new UIValidationEvent("form-1", false, errors, timestamp);
        
        // Assert
        Assert.Equal("validation", evt.EventType);
        Assert.Equal("form-1", evt.ComponentId);
        Assert.False(evt.IsValid);
        Assert.Equal(2, evt.ValidationErrors!.Length);
        Assert.Equal("Required field", evt.ValidationErrors[0]);
        Assert.Equal("Invalid format", evt.ValidationErrors[1]);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    [Fact]
    public void UILifecycleEvent_ShouldInitializeCorrectly()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        
        // Act
        var evt = new UILifecycleEvent("comp-1", "mounted", timestamp);
        
        // Assert
        Assert.Equal("lifecycle", evt.EventType);
        Assert.Equal("comp-1", evt.ComponentId);
        Assert.Equal("mounted", evt.LifecycleStage);
        Assert.Equal(timestamp, evt.Timestamp);
    }

    // Helper class for testing
    private record TestUIEvent(
        string EventType,
        DateTime Timestamp,
        string? ComponentId = null,
        Dictionary<string, object>? Data = null
    ) : UIEvent(EventType, Timestamp, ComponentId, Data);
}