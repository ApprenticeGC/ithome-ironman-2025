using GameConsole.UI.Console;
using GameConsole.Input.Core;

namespace GameConsole.UI.Console.Tests;

public class ConsoleMenuTests
{
    [Fact]
    public void Constructor_WithParameters_SetsProperties()
    {
        var menu = new ConsoleMenu("Test Menu", ANSIEscapeSequences.FgGreen, 
                                  ANSIEscapeSequences.BgBlack, ANSIEscapeSequences.FgYellow);
        
        Assert.Empty(menu.Items);
        Assert.Equal(0, menu.SelectedIndex);
        Assert.Null(menu.SelectedItem);
    }
    
    [Fact]
    public void AddItem_WithValidItem_AddsToItems()
    {
        var menu = new ConsoleMenu();
        var item = new ConsoleMenuItem("Test Item", () => { }, true, 't');
        
        menu.AddItem(item);
        
        Assert.Single(menu.Items);
        Assert.Equal(item, menu.Items[0]);
        Assert.Equal(item, menu.SelectedItem);
    }
    
    [Fact]
    public void AddItem_WithStringAndAction_CreatesAndAddsItem()
    {
        var menu = new ConsoleMenu();
        var actionCalled = false;
        
        var item = menu.AddItem("Test", () => actionCalled = true, 't');
        
        Assert.Single(menu.Items);
        Assert.Equal("Test", item.Text);
        Assert.Equal('t', item.Hotkey);
        
        item.Action?.Invoke();
        Assert.True(actionCalled);
    }
    
    [Fact]
    public void AddSeparator_WhenCalled_AddsSeparatorItem()
    {
        var menu = new ConsoleMenu();
        
        menu.AddSeparator("--- Section ---");
        
        Assert.Single(menu.Items);
        Assert.True(menu.Items[0].IsSeparator);
        Assert.Equal("--- Section ---", menu.Items[0].Text);
    }
    
    [Fact]
    public void SelectedIndex_WithValidIndex_SetsCorrectly()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        menu.AddItem("Item 3");
        
        menu.SelectedIndex = 2;
        
        Assert.Equal(2, menu.SelectedIndex);
        Assert.Equal("Item 3", menu.SelectedItem?.Text);
    }
    
    [Fact]
    public void SelectedIndex_WithInvalidIndex_ClampsToBounds()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        
        menu.SelectedIndex = 5; // Too high
        Assert.Equal(1, menu.SelectedIndex);
        
        menu.SelectedIndex = -1; // Too low
        Assert.Equal(0, menu.SelectedIndex);
    }
    
    [Fact]
    public void MoveToNext_WithMultipleItems_MovesToNextItem()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        menu.AddItem("Item 3");
        
        menu.MoveToNext();
        Assert.Equal(1, menu.SelectedIndex);
        
        menu.MoveToNext();
        Assert.Equal(2, menu.SelectedIndex);
        
        menu.MoveToNext(); // Should wrap to 0
        Assert.Equal(0, menu.SelectedIndex);
    }
    
    [Fact]
    public void MoveToPrevious_WithMultipleItems_MovesToPreviousItem()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        menu.AddItem("Item 3");
        menu.SelectedIndex = 2;
        
        menu.MoveToPrevious();
        Assert.Equal(1, menu.SelectedIndex);
        
        menu.MoveToPrevious();
        Assert.Equal(0, menu.SelectedIndex);
        
        menu.MoveToPrevious(); // Should wrap to 2
        Assert.Equal(2, menu.SelectedIndex);
    }
    
    [Fact]
    public void MoveToNext_WithSeparators_SkipsSeparators()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddSeparator();
        menu.AddItem("Item 2");
        
        menu.MoveToNext();
        
        Assert.Equal(2, menu.SelectedIndex); // Should skip separator at index 1
        Assert.Equal("Item 2", menu.SelectedItem?.Text);
    }
    
    [Fact]
    public void HandleKeyInput_WithDownArrow_MovesToNext()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        
        var handled = menu.HandleKeyInput(KeyCode.DownArrow);
        
        Assert.True(handled);
        Assert.Equal(1, menu.SelectedIndex);
    }
    
    [Fact]
    public void HandleKeyInput_WithUpArrow_MovesToPrevious()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        menu.SelectedIndex = 1;
        
        var handled = menu.HandleKeyInput(KeyCode.UpArrow);
        
        Assert.True(handled);
        Assert.Equal(0, menu.SelectedIndex);
    }
    
    [Fact]
    public void HandleKeyInput_WithEnter_SelectsCurrentItem()
    {
        var menu = new ConsoleMenu();
        var actionCalled = false;
        var itemSelectedEventRaised = false;
        
        menu.AddItem("Test Item", () => actionCalled = true);
        menu.ItemSelected += (sender, item) => itemSelectedEventRaised = true;
        
        var handled = menu.HandleKeyInput(KeyCode.Enter);
        
        Assert.True(handled);
        Assert.True(actionCalled);
        Assert.True(itemSelectedEventRaised);
    }
    
    [Fact]
    public void HandleKeyInput_WithHotkey_SelectsItemWithHotkey()
    {
        var menu = new ConsoleMenu();
        var item1ActionCalled = false;
        var item2ActionCalled = false;
        
        menu.AddItem("Item 1", () => item1ActionCalled = true, 'a');
        menu.AddItem("Item 2", () => item2ActionCalled = true, 'b');
        
        var handled = menu.HandleKeyInput(KeyCode.B);
        
        Assert.True(handled);
        Assert.False(item1ActionCalled);
        Assert.True(item2ActionCalled);
        Assert.Equal(1, menu.SelectedIndex);
    }
    
    [Fact]
    public void GetDesiredSize_WithItems_ReturnsCorrectSize()
    {
        var menu = new ConsoleMenu("Menu Title");
        menu.AddItem("Short");
        menu.AddItem("Very Long Item Text");
        menu.AddSeparator();
        
        var size = menu.GetDesiredSize(new ConsoleSize(100, 100));
        
        // Should account for title (+ separator) and 3 items
        Assert.Equal(5, size.Height); // Title + separator + 3 items
        Assert.True(size.Width > 0);
    }
    
    [Fact]
    public void RemoveItem_WithExistingItem_RemovesItem()
    {
        var menu = new ConsoleMenu();
        var item1 = menu.AddItem("Item 1");
        var item2 = menu.AddItem("Item 2");
        
        var removed = menu.RemoveItem(item1);
        
        Assert.True(removed);
        Assert.Single(menu.Items);
        Assert.Equal(item2, menu.Items[0]);
    }
    
    [Fact]
    public void ClearItems_WhenCalled_RemovesAllItems()
    {
        var menu = new ConsoleMenu();
        menu.AddItem("Item 1");
        menu.AddItem("Item 2");
        
        menu.ClearItems();
        
        Assert.Empty(menu.Items);
        Assert.Equal(0, menu.SelectedIndex);
    }
}