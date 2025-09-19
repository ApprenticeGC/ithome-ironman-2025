using FluentAssertions;
using GameConsole.UI.Core;
using System.Reactive.Linq;
using Microsoft.Reactive.Testing;
using Xunit;

namespace GameConsole.UI.Core.Tests;

// Test implementation of IUIComponent for testing purposes
public class TestUIComponent : UIComponentBase
{
    public override string Type => "TestComponent";

    public override Task RenderAsync(UIContext context, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    public override Task<(float Width, float Height)> MeasureAsync((float Width, float Height) availableSize, UIContext context, CancellationToken cancellationToken = default)
    {
        return Task.FromResult((100f, 50f));
    }

    public override Task ArrangeAsync(UILayout bounds, UIContext context, CancellationToken cancellationToken = default)
    {
        Layout = bounds;
        return Task.CompletedTask;
    }

    // Expose protected methods for testing
    public new void PublishEvent(UIEvent uiEvent) => base.PublishEvent(uiEvent);
    public new void OnClickEvent((float X, float Y)? position = null, int button = 0) => base.OnClickEvent(position, button);
    public new void OnValueChangedEvent(object? oldValue, object? newValue) => base.OnValueChangedEvent(oldValue, newValue);
}

public class UIComponentTests
{
    [Fact]
    public void UIComponent_Should_Have_Unique_Id()
    {
        // Arrange & Act
        var component1 = new TestUIComponent();
        var component2 = new TestUIComponent();

        // Assert
        component1.Id.Should().NotBeNullOrEmpty();
        component2.Id.Should().NotBeNullOrEmpty();
        component1.Id.Should().NotBe(component2.Id);
    }

    [Fact]
    public void UIComponent_Should_Initialize_With_Default_Values()
    {
        // Arrange & Act
        var component = new TestUIComponent();

        // Assert
        component.Type.Should().Be("TestComponent");
        component.IsEnabled.Should().BeTrue();
        component.HasFocus.Should().BeFalse();
        component.Parent.Should().BeNull();
        component.Children.Should().BeEmpty();
    }

    [Fact]
    public async Task UIComponent_Should_Support_Adding_And_Removing_Children()
    {
        // Arrange
        var parent = new TestUIComponent();
        var child1 = new TestUIComponent();
        var child2 = new TestUIComponent();

        // Act
        await parent.AddChildAsync(child1);
        await parent.AddChildAsync(child2);

        // Assert
        parent.Children.Should().HaveCount(2);
        parent.Children.Should().Contain(child1);
        parent.Children.Should().Contain(child2);

        // Act - Remove child
        await parent.RemoveChildAsync(child1);

        // Assert
        parent.Children.Should().HaveCount(1);
        parent.Children.Should().Contain(child2);
        parent.Children.Should().NotContain(child1);
    }

    [Fact]
    public async Task UIComponent_Should_Focus_Correctly()
    {
        // Arrange
        var component = new TestUIComponent();
        var focusEvents = new List<UIFocusEvent>();
        
        component.Events.OfType<UIFocusEvent>().Subscribe(focusEvents.Add);

        // Act
        await component.FocusAsync();

        // Assert
        component.HasFocus.Should().BeTrue();
        focusEvents.Should().HaveCount(1);
        focusEvents[0].HasFocus.Should().BeTrue();
        focusEvents[0].ComponentId.Should().Be(component.Id);
    }

    [Fact]
    public async Task UIComponent_Should_Measure_And_Arrange()
    {
        // Arrange
        var component = new TestUIComponent();
        var context = new UIContext
        {
            Framework = UIFrameworkType.Console,
            SupportedCapabilities = UICapabilities.TextDisplay,
            Viewport = new UILayout { Width = 800, Height = 600 }
        };

        // Act - Measure
        var size = await component.MeasureAsync((200, 100), context);

        // Assert
        size.Should().Be((100f, 50f));

        // Act - Arrange
        var bounds = new UILayout { X = 10, Y = 20, Width = 100, Height = 50 };
        await component.ArrangeAsync(bounds, context);

        // Assert
        component.Layout.Should().Be(bounds);
    }

    [Fact]
    public void UIComponent_Should_Publish_Events()
    {
        // Arrange
        var component = new TestUIComponent();
        var events = new List<UIEvent>();
        
        component.Events.Subscribe(events.Add);

        // Act
        component.OnClickEvent((50, 75), 1);
        component.OnValueChangedEvent("old", "new");

        // Assert
        events.Should().HaveCount(2);
        
        var clickEvent = events.OfType<UIClickEvent>().First();
        clickEvent.ComponentId.Should().Be(component.Id);
        clickEvent.Position.Should().Be((50f, 75f));
        clickEvent.Button.Should().Be(1);

        var valueEvent = events.OfType<UIValueChangedEvent>().First();
        valueEvent.ComponentId.Should().Be(component.Id);
        valueEvent.OldValue.Should().Be("old");
        valueEvent.NewValue.Should().Be("new");
    }

    [Fact]
    public void UIComponent_Should_Support_Data_Context()
    {
        // Arrange
        var component = new TestUIComponent();
        var dataContext = new { Name = "Test", Value = 42 };

        // Act
        component.DataContext = dataContext;

        // Assert
        component.DataContext.Should().Be(dataContext);
    }

    [Fact]
    public void UIComponent_Should_Support_Custom_Properties()
    {
        // Arrange
        var component = new TestUIComponent();
        var properties = new Dictionary<string, object>
        {
            ["CustomProperty1"] = "value1",
            ["CustomProperty2"] = 123
        };

        // Act
        component.Properties = properties;

        // Assert
        component.Properties.Should().BeSameAs(properties);
        component.Properties!["CustomProperty1"].Should().Be("value1");
        component.Properties["CustomProperty2"].Should().Be(123);
    }

    [Fact]
    public async Task UIComponent_Should_Dispose_Properly()
    {
        // Arrange
        var parent = new TestUIComponent();
        var child = new TestUIComponent();
        var events = new List<UILifecycleEvent>();
        
        parent.Events.OfType<UILifecycleEvent>().Subscribe(events.Add);
        await parent.AddChildAsync(child);

        // Act
        await parent.DisposeAsync();

        // Assert
        var lifecycleEvents = events.Where(e => e.Stage == UILifecycleStage.Disposing || e.Stage == UILifecycleStage.Disposed).ToList();
        lifecycleEvents.Should().HaveCount(2);
        lifecycleEvents.Should().Contain(e => e.Stage == UILifecycleStage.Disposing);
        lifecycleEvents.Should().Contain(e => e.Stage == UILifecycleStage.Disposed);
    }
}