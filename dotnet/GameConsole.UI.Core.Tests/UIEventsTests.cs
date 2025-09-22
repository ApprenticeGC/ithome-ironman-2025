using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI event classes and their properties.
/// </summary>
public class UIEventsTests
{
    [Fact]
    public void UIFocusEvent_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var timestamp = DateTime.UtcNow;
        var elementId = "test-button";
        var previousElementId = "previous-button";
        var frameNumber = 1234L;

        // Act
        var focusEvent = new UIFocusEvent
        {
            ElementId = elementId,
            PreviousElementId = previousElementId,
            Timestamp = timestamp,
            FrameNumber = frameNumber,
            SourceElementId = elementId
        };

        // Assert
        Assert.Equal(elementId, focusEvent.ElementId);
        Assert.Equal(previousElementId, focusEvent.PreviousElementId);
        Assert.Equal(timestamp, focusEvent.Timestamp);
        Assert.Equal(frameNumber, focusEvent.FrameNumber);
        Assert.Equal(elementId, focusEvent.SourceElementId);
    }

    [Fact]
    public void UIActivationEvent_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var elementId = "test-button";
        var method = UIActivationMethod.Mouse;
        var position = new UIPosition(10, 20);
        var timestamp = DateTime.UtcNow;

        // Act
        var activationEvent = new UIActivationEvent
        {
            ElementId = elementId,
            Method = method,
            Position = position,
            Timestamp = timestamp
        };

        // Assert
        Assert.Equal(elementId, activationEvent.ElementId);
        Assert.Equal(method, activationEvent.Method);
        Assert.Equal(position, activationEvent.Position);
        Assert.Equal(timestamp, activationEvent.Timestamp);
    }

    [Fact]
    public void UILayoutEvent_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var elementId = "test-panel";
        var newBounds = new UIRect(10, 20, 100, 50);
        var previousBounds = new UIRect(5, 10, 80, 40);

        // Act
        var layoutEvent = new UILayoutEvent
        {
            ElementId = elementId,
            NewBounds = newBounds,
            PreviousBounds = previousBounds
        };

        // Assert
        Assert.Equal(elementId, layoutEvent.ElementId);
        Assert.Equal(newBounds, layoutEvent.NewBounds);
        Assert.Equal(previousBounds, layoutEvent.PreviousBounds);
    }

    [Fact]
    public void UITextInputEvent_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var text = "Hello World";
        var elementId = "text-input";
        var cursorPosition = 5;

        // Act
        var textInputEvent = new UITextInputEvent
        {
            Text = text,
            ElementId = elementId,
            CursorPosition = cursorPosition
        };

        // Assert
        Assert.Equal(text, textInputEvent.Text);
        Assert.Equal(elementId, textInputEvent.ElementId);
        Assert.Equal(cursorPosition, textInputEvent.CursorPosition);
    }

    [Fact]
    public void UIStateChangeEvent_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var elementId = "test-button";
        var newState = UIState.Focused;
        var previousState = UIState.Normal;

        // Act
        var stateChangeEvent = new UIStateChangeEvent
        {
            ElementId = elementId,
            NewState = newState,
            PreviousState = previousState
        };

        // Assert
        Assert.Equal(elementId, stateChangeEvent.ElementId);
        Assert.Equal(newState, stateChangeEvent.NewState);
        Assert.Equal(previousState, stateChangeEvent.PreviousState);
    }

    [Theory]
    [InlineData(UIActivationMethod.Mouse)]
    [InlineData(UIActivationMethod.Keyboard)]
    [InlineData(UIActivationMethod.Touch)]
    [InlineData(UIActivationMethod.Gamepad)]
    public void UIActivationMethod_AllValues_AreDefined(UIActivationMethod method)
    {
        // Act & Assert - Should not throw
        var name = method.ToString();
        Assert.NotEmpty(name);
    }

    [Fact]
    public void UIEvent_BaseClass_HasDefaultTimestamp()
    {
        // Arrange & Act
        var testEvent = new TestUIEvent();

        // Assert
        Assert.True(testEvent.Timestamp <= DateTime.UtcNow);
        Assert.True(testEvent.Timestamp > DateTime.UtcNow.AddSeconds(-5)); // Should be recent
    }

    [Fact]
    public void UIEvent_FrameNumber_DefaultsToZero()
    {
        // Arrange & Act
        var testEvent = new TestUIEvent();

        // Assert
        Assert.Equal(0, testEvent.FrameNumber);
    }

    [Fact]
    public void UIEvent_SourceElementId_CanBeNull()
    {
        // Arrange & Act
        var testEvent = new TestUIEvent();

        // Assert
        Assert.Null(testEvent.SourceElementId);
    }

    // Test implementation of abstract UIEvent class
    private class TestUIEvent : UIEvent
    {
    }
}