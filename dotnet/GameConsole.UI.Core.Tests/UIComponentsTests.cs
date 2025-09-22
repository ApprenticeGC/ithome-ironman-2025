using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI component interfaces and contracts.
/// </summary>
public class UIComponentsTests
{
    [Fact]
    public void ComponentStateChangedEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var componentId = "test-component";
        var oldState = ComponentState.Normal;
        var newState = ComponentState.Focused;
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new ComponentStateChangedEventArgs(componentId, oldState, newState);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(componentId, args.ComponentId);
        Assert.Equal(oldState, args.OldState);
        Assert.Equal(newState, args.NewState);
        Assert.True(args.ChangeTime >= beforeCreate);
        Assert.True(args.ChangeTime <= afterCreate);
    }
    
    [Fact]
    public void LayoutInvalidatedEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var reason = "Size changed";
        var componentId = "test-component";
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new LayoutInvalidatedEventArgs(reason, componentId);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(reason, args.Reason);
        Assert.Equal(componentId, args.ComponentId);
        Assert.True(args.InvalidationTime >= beforeCreate);
        Assert.True(args.InvalidationTime <= afterCreate);
    }
    
    [Fact]
    public void LayoutInvalidatedEventArgs_ShouldCreateWithoutComponentId()
    {
        // Arrange
        var reason = "Global layout change";
        
        // Act
        var args = new LayoutInvalidatedEventArgs(reason);
        
        // Assert
        Assert.Equal(reason, args.Reason);
        Assert.Null(args.ComponentId);
    }
    
    [Fact]
    public void ComponentClickEventArgs_ShouldCreateWithDefaults()
    {
        // Arrange
        var position = new ConsolePosition(10, 20);
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new ComponentClickEventArgs(position);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(position, args.Position);
        Assert.Equal(MouseButton.Left, args.Button); // Default button
        Assert.True(args.ClickTime >= beforeCreate);
        Assert.True(args.ClickTime <= afterCreate);
    }
    
    [Fact]
    public void ComponentClickEventArgs_ShouldCreateWithCustomButton()
    {
        // Arrange
        var position = new ConsolePosition(5, 15);
        var button = MouseButton.Right;
        
        // Act
        var args = new ComponentClickEventArgs(position, button);
        
        // Assert
        Assert.Equal(position, args.Position);
        Assert.Equal(button, args.Button);
    }
    
    [Fact]
    public void ComponentKeyEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var keyInfo = new ConsoleKeyInfo('a', ConsoleKey.A, false, false, false);
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new ComponentKeyEventArgs(keyInfo);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(keyInfo, args.KeyInfo);
        Assert.False(args.Handled); // Should default to false
        Assert.True(args.KeyTime >= beforeCreate);
        Assert.True(args.KeyTime <= afterCreate);
    }
    
    [Fact]
    public void ComponentKeyEventArgs_HandledProperty_ShouldBeSettable()
    {
        // Arrange
        var keyInfo = new ConsoleKeyInfo('b', ConsoleKey.B, false, false, false);
        var args = new ComponentKeyEventArgs(keyInfo);
        
        // Act
        args.Handled = true;
        
        // Assert
        Assert.True(args.Handled);
    }
    
    [Fact]
    public void InputValueChangedEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var oldValue = "old text";
        var newValue = "new text";
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new InputValueChangedEventArgs(oldValue, newValue);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(oldValue, args.OldValue);
        Assert.Equal(newValue, args.NewValue);
        Assert.True(args.ChangeTime >= beforeCreate);
        Assert.True(args.ChangeTime <= afterCreate);
    }
    
    [Fact]
    public void SelectionChangedEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var wasSelected = false;
        var isSelected = true;
        var value = "selected item";
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new SelectionChangedEventArgs(wasSelected, isSelected, value);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(wasSelected, args.WasSelected);
        Assert.Equal(isSelected, args.IsSelected);
        Assert.Equal(value, args.Value);
        Assert.True(args.ChangeTime >= beforeCreate);
        Assert.True(args.ChangeTime <= afterCreate);
    }
    
    [Fact]
    public void SelectionChangedEventArgs_ShouldCreateWithoutValue()
    {
        // Arrange
        var wasSelected = true;
        var isSelected = false;
        
        // Act
        var args = new SelectionChangedEventArgs(wasSelected, isSelected);
        
        // Assert
        Assert.Equal(wasSelected, args.WasSelected);
        Assert.Equal(isSelected, args.IsSelected);
        Assert.Null(args.Value);
    }
    
    [Fact]
    public void DialogClosingEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var result = DialogResult.OK;
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new DialogClosingEventArgs(result);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(result, args.Result);
        Assert.False(args.Cancel); // Should default to false
        Assert.True(args.ClosingTime >= beforeCreate);
        Assert.True(args.ClosingTime <= afterCreate);
    }
    
    [Fact]
    public void DialogClosingEventArgs_CancelProperty_ShouldBeSettable()
    {
        // Arrange
        var args = new DialogClosingEventArgs(DialogResult.Cancel);
        
        // Act
        args.Cancel = true;
        
        // Assert
        Assert.True(args.Cancel);
    }
    
    [Fact]
    public void DialogClosedEventArgs_ShouldCreateCorrectly()
    {
        // Arrange
        var result = DialogResult.Yes;
        var beforeCreate = DateTime.UtcNow;
        
        // Act
        var args = new DialogClosedEventArgs(result);
        var afterCreate = DateTime.UtcNow;
        
        // Assert
        Assert.Equal(result, args.Result);
        Assert.True(args.ClosedTime >= beforeCreate);
        Assert.True(args.ClosedTime <= afterCreate);
    }
    
    [Theory]
    [InlineData(ComponentState.Normal)]
    [InlineData(ComponentState.Focused)]
    [InlineData(ComponentState.Pressed)]
    [InlineData(ComponentState.Hovered)]
    [InlineData(ComponentState.Disabled)]
    [InlineData(ComponentState.Selected)]
    public void ComponentState_AllValuesSupported(ComponentState state)
    {
        // This test ensures all component state values are properly defined
        Assert.True(Enum.IsDefined(typeof(ComponentState), state));
    }
    
    [Theory]
    [InlineData(Visibility.Visible)]
    [InlineData(Visibility.Hidden)]
    [InlineData(Visibility.Collapsed)]
    public void Visibility_AllValuesSupported(Visibility visibility)
    {
        // This test ensures all visibility values are properly defined
        Assert.True(Enum.IsDefined(typeof(Visibility), visibility));
    }
    
    [Theory]
    [InlineData(LayoutDirection.Horizontal)]
    [InlineData(LayoutDirection.Vertical)]
    public void LayoutDirection_AllValuesSupported(LayoutDirection direction)
    {
        // This test ensures all layout direction values are properly defined
        Assert.True(Enum.IsDefined(typeof(LayoutDirection), direction));
    }
    
    [Theory]
    [InlineData(HorizontalAlignment.Left)]
    [InlineData(HorizontalAlignment.Center)]
    [InlineData(HorizontalAlignment.Right)]
    [InlineData(HorizontalAlignment.Stretch)]
    public void HorizontalAlignment_AllValuesSupported(HorizontalAlignment alignment)
    {
        // This test ensures all horizontal alignment values are properly defined
        Assert.True(Enum.IsDefined(typeof(HorizontalAlignment), alignment));
    }
    
    [Theory]
    [InlineData(VerticalAlignment.Top)]
    [InlineData(VerticalAlignment.Middle)]
    [InlineData(VerticalAlignment.Bottom)]
    [InlineData(VerticalAlignment.Stretch)]
    public void VerticalAlignment_AllValuesSupported(VerticalAlignment alignment)
    {
        // This test ensures all vertical alignment values are properly defined
        Assert.True(Enum.IsDefined(typeof(VerticalAlignment), alignment));
    }
}