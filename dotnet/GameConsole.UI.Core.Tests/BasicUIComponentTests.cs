using Xunit;
using GameConsole.UI.Core;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for the BasicUIComponent class functionality.
/// </summary>
public class BasicUIComponentTests
{
    [Fact]
    public void Constructor_ShouldSetIdAndComponentType()
    {
        // Arrange
        const string id = "test-id";
        const string componentType = "test-type";
        
        // Act
        var component = new BasicUIComponent(id, componentType);
        
        // Assert
        Assert.Equal(id, component.Id);
        Assert.Equal(componentType, component.ComponentType);
        Assert.True(component.IsVisible);
        Assert.True(component.IsEnabled);
        Assert.Empty(component.Children);
        Assert.Empty(component.Properties);
    }
    
    [Fact]
    public void Data_WhenSet_ShouldRaiseDataChangedEvent()
    {
        // Arrange
        var component = new BasicUIComponent("test", "text");
        UIDataChangedEventArgs? eventArgs = null;
        component.DataChanged += (_, args) => eventArgs = args;
        const string testData = "test data";
        
        // Act
        component.Data = testData;
        
        // Assert
        Assert.Equal(testData, component.Data);
        Assert.NotNull(eventArgs);
        Assert.Null(eventArgs.PreviousData);
        Assert.Equal(testData, eventArgs.NewData);
    }
    
    [Fact]
    public async Task UpdateAsync_ShouldSetDataAndRaiseEvents()
    {
        // Arrange
        var component = new BasicUIComponent("test", "button");
        UIDataChangedEventArgs? dataChangedArgs = null;
        UIComponentEventArgs? componentEventArgs = null;
        
        component.DataChanged += (_, args) => dataChangedArgs = args;
        component.ComponentEvent += (_, args) => componentEventArgs = args;
        
        const string newData = "updated data";
        
        // Act
        await component.UpdateAsync(newData);
        
        // Assert
        Assert.Equal(newData, component.Data);
        Assert.NotNull(dataChangedArgs);
        Assert.NotNull(componentEventArgs);
        Assert.Equal("updated", componentEventArgs.EventType);
        Assert.Equal(newData, componentEventArgs.EventData);
    }
    
    [Fact]
    public async Task SetPropertyAsync_ShouldSetPropertyAndRaiseEvent()
    {
        // Arrange
        var component = new BasicUIComponent("test", "input");
        UIComponentEventArgs? eventArgs = null;
        component.ComponentEvent += (_, args) => eventArgs = args;
        
        const string key = "placeholder";
        const string value = "Enter text here";
        
        // Act
        await component.SetPropertyAsync(key, value);
        
        // Assert
        Assert.Equal(value, component.GetProperty<string>(key));
        Assert.NotNull(eventArgs);
        Assert.Equal("property_changed", eventArgs.EventType);
    }
    
    [Fact]
    public async Task GetProperty_WithExistingProperty_ShouldReturnValue()
    {
        // Arrange
        var component = new BasicUIComponent("test", "container");
        const string key = "width";
        const int value = 100;
        
        // Act
        await component.SetPropertyAsync(key, value);
        var result = component.GetProperty<int>(key);
        
        // Assert
        Assert.Equal(value, result);
    }
    
    [Fact]
    public void GetProperty_WithNonExistentProperty_ShouldReturnDefaultValue()
    {
        // Arrange
        var component = new BasicUIComponent("test", "list");
        const string defaultValue = "default";
        
        // Act
        var result = component.GetProperty("nonexistent", defaultValue);
        
        // Assert
        Assert.Equal(defaultValue, result);
    }
    
    [Fact]
    public async Task AddChildAsync_ShouldAddChildAndRaiseEvent()
    {
        // Arrange
        var parent = new BasicUIComponent("parent", "container");
        var child = new BasicUIComponent("child", "text");
        UIComponentEventArgs? eventArgs = null;
        parent.ComponentEvent += (_, args) => eventArgs = args;
        
        // Act
        await parent.AddChildAsync(child);
        
        // Assert
        Assert.Single(parent.Children);
        Assert.Equal(child, parent.Children[0]);
        Assert.NotNull(eventArgs);
        Assert.Equal("child_added", eventArgs.EventType);
        Assert.Equal(child, eventArgs.EventData);
    }
    
    [Fact]
    public async Task RemoveChildAsync_ShouldRemoveChildAndRaiseEvent()
    {
        // Arrange
        var parent = new BasicUIComponent("parent", "container");
        var child = new BasicUIComponent("child", "button");
        await parent.AddChildAsync(child);
        
        UIComponentEventArgs? eventArgs = null;
        parent.ComponentEvent += (_, args) => 
        {
            if (args.EventType == "child_removed") eventArgs = args;
        };
        
        // Act
        var result = await parent.RemoveChildAsync(child);
        
        // Assert
        Assert.True(result);
        Assert.Empty(parent.Children);
        Assert.NotNull(eventArgs);
        Assert.Equal("child_removed", eventArgs.EventType);
        Assert.Equal(child, eventArgs.EventData);
    }
    
    [Fact]
    public async Task SetVisibilityAsync_ShouldChangeVisibilityAndRaiseEvent()
    {
        // Arrange
        var component = new BasicUIComponent("test", "menu");
        UIComponentEventArgs? eventArgs = null;
        component.ComponentEvent += (_, args) => eventArgs = args;
        
        // Act
        await component.SetVisibilityAsync(false);
        
        // Assert
        Assert.False(component.IsVisible);
        Assert.NotNull(eventArgs);
        Assert.Equal("visibility_changed", eventArgs.EventType);
        Assert.Equal(false, eventArgs.EventData);
    }
    
    [Fact]
    public async Task SetEnabledAsync_ShouldChangeEnabledStateAndRaiseEvent()
    {
        // Arrange
        var component = new BasicUIComponent("test", "progress");
        UIComponentEventArgs? eventArgs = null;
        component.ComponentEvent += (_, args) => eventArgs = args;
        
        // Act
        await component.SetEnabledAsync(false);
        
        // Assert
        Assert.False(component.IsEnabled);
        Assert.NotNull(eventArgs);
        Assert.Equal("enabled_changed", eventArgs.EventType);
        Assert.Equal(false, eventArgs.EventData);
    }
    
    [Fact]
    public async Task RenderAsync_ShouldReturnRenderDataAndRaiseEvent()
    {
        // Arrange
        var component = new BasicUIComponent("test", "table");
        var context = UIContext.Create(UIFrameworkType.Console, UICapabilities.TableDisplay);
        UIComponentEventArgs? eventArgs = null;
        component.ComponentEvent += (_, args) => eventArgs = args;
        
        // Act
        var renderData = await component.RenderAsync(context);
        
        // Assert
        Assert.NotNull(renderData);
        Assert.NotNull(eventArgs);
        Assert.Equal("rendered", eventArgs.EventType);
        Assert.Equal(renderData, eventArgs.EventData);
    }
    
    [Fact]
    public async Task DisposeAsync_ShouldDisposeChildrenAndRaiseEvent()
    {
        // Arrange
        var parent = new BasicUIComponent("parent", "container");
        var child = new BasicUIComponent("child", "text");
        await parent.AddChildAsync(child);
        
        UIComponentEventArgs? eventArgs = null;
        parent.ComponentEvent += (_, args) => 
        {
            if (args.EventType == "disposed") eventArgs = args;
        };
        
        // Act
        await parent.DisposeAsync();
        
        // Assert
        Assert.Empty(parent.Children);
        Assert.Empty(parent.Properties);
        Assert.NotNull(eventArgs);
        Assert.Equal("disposed", eventArgs.EventType);
    }
}