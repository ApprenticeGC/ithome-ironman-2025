using GameConsole.UI.Core;
using Xunit;

namespace GameConsole.UI.Core.Tests;

/// <summary>
/// Tests for UI model classes and their behavior.
/// </summary>
public class UIModelsTests
{
    [Fact]
    public void UIElement_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var button = new UIButton
        {
            Id = "test-button",
            Text = "Click Me",
            Position = new UIPosition(10, 20),
            Size = new UISize(100, 30),
            State = UIState.Normal,
            IsVisible = true,
            CanFocus = true,
            ParentId = "parent-panel",
            ZOrder = 5
        };

        // Assert
        Assert.Equal("test-button", button.Id);
        Assert.Equal("Click Me", button.Text);
        Assert.Equal(new UIPosition(10, 20), button.Position);
        Assert.Equal(new UISize(100, 30), button.Size);
        Assert.Equal(UIState.Normal, button.State);
        Assert.True(button.IsVisible);
        Assert.True(button.CanFocus);
        Assert.Equal("parent-panel", button.ParentId);
        Assert.Equal(5, button.ZOrder);
    }

    [Fact]
    public void UIButton_SpecificProperties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var button = new UIButton
        {
            OnClickAction = "SaveData",
            Shortcut = "Ctrl+S"
        };

        // Assert
        Assert.Equal("SaveData", button.OnClickAction);
        Assert.Equal("Ctrl+S", button.Shortcut);
    }

    [Fact]
    public void UITextBox_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var textBox = new UITextBox
        {
            Id = "username-input",
            Text = "John Doe",
            PlaceholderText = "Enter your name",
            CursorPosition = 4,
            IsReadOnly = false,
            MaxLength = 50,
            IsMasked = false,
            MaskCharacter = '*'
        };

        // Assert
        Assert.Equal("username-input", textBox.Id);
        Assert.Equal("John Doe", textBox.Text);
        Assert.Equal("Enter your name", textBox.PlaceholderText);
        Assert.Equal(4, textBox.CursorPosition);
        Assert.False(textBox.IsReadOnly);
        Assert.Equal(50, textBox.MaxLength);
        Assert.False(textBox.IsMasked);
        Assert.Equal('*', textBox.MaskCharacter);
    }

    [Fact]
    public void UITextBox_DefaultMaxLength_IsMaxInt()
    {
        // Arrange & Act
        var textBox = new UITextBox();

        // Assert
        Assert.Equal(int.MaxValue, textBox.MaxLength);
    }

    [Fact]
    public void UILabel_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var label = new UILabel
        {
            Id = "status-label",
            Text = "Status: Ready",
            Alignment = UIAlignment.Center,
            WordWrap = true
        };

        // Assert
        Assert.Equal("status-label", label.Id);
        Assert.Equal("Status: Ready", label.Text);
        Assert.Equal(UIAlignment.Center, label.Alignment);
        Assert.True(label.WordWrap);
    }

    [Fact]
    public void UILabel_DefaultAlignment_IsLeft()
    {
        // Arrange & Act
        var label = new UILabel();

        // Assert
        Assert.Equal(UIAlignment.Left, label.Alignment);
    }

    [Fact]
    public void UIPanel_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var panel = new UIPanel
        {
            Id = "main-panel",
            HasBorder = true,
            Title = "Main Content"
        };

        // Add child elements
        panel.ChildElementIds.Add("child1");
        panel.ChildElementIds.Add("child2");

        // Assert
        Assert.Equal("main-panel", panel.Id);
        Assert.True(panel.HasBorder);
        Assert.Equal("Main Content", panel.Title);
        Assert.Contains("child1", panel.ChildElementIds);
        Assert.Contains("child2", panel.ChildElementIds);
        Assert.Equal(2, panel.ChildElementIds.Count);
    }

    [Fact]
    public void UIPanel_DefaultHasBorder_IsTrue()
    {
        // Arrange & Act
        var panel = new UIPanel();

        // Assert
        Assert.True(panel.HasBorder);
    }

    [Fact]
    public void UIMenu_Properties_CanBeSetAndRetrieved()
    {
        // Arrange
        var menuItem1 = new UIMenuItem 
        { 
            Id = "item1", 
            Text = "New File",
            OnSelectAction = "CreateNewFile",
            Shortcut = "Ctrl+N"
        };
        var menuItem2 = new UIMenuItem 
        { 
            Id = "item2", 
            Text = "Open File",
            OnSelectAction = "OpenFile",
            Shortcut = "Ctrl+O"
        };

        // Act
        var menu = new UIMenu
        {
            Id = "file-menu",
            SelectedIndex = 1,
            AllowMultiSelect = false
        };
        menu.Items.Add(menuItem1);
        menu.Items.Add(menuItem2);

        // Assert
        Assert.Equal("file-menu", menu.Id);
        Assert.Equal(1, menu.SelectedIndex);
        Assert.False(menu.AllowMultiSelect);
        Assert.Equal(2, menu.Items.Count);
        Assert.Contains(menuItem1, menu.Items);
        Assert.Contains(menuItem2, menu.Items);
    }

    [Fact]
    public void UIMenuItem_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var menuItem = new UIMenuItem
        {
            Id = "save-item",
            Text = "Save",
            OnSelectAction = "SaveDocument",
            IsEnabled = true,
            IsSelected = false,
            Shortcut = "Ctrl+S"
        };

        // Assert
        Assert.Equal("save-item", menuItem.Id);
        Assert.Equal("Save", menuItem.Text);
        Assert.Equal("SaveDocument", menuItem.OnSelectAction);
        Assert.True(menuItem.IsEnabled);
        Assert.False(menuItem.IsSelected);
        Assert.Equal("Ctrl+S", menuItem.Shortcut);
    }

    [Fact]
    public void UIMenuItem_DefaultIsEnabled_IsTrue()
    {
        // Arrange & Act
        var menuItem = new UIMenuItem();

        // Assert
        Assert.True(menuItem.IsEnabled);
    }

    [Fact]
    public void UIProgressBar_Properties_CanBeSetAndRetrieved()
    {
        // Arrange & Act
        var progressBar = new UIProgressBar
        {
            Id = "download-progress",
            Value = 0.75f,
            Minimum = 0.0f,
            Maximum = 1.0f,
            ShowPercentage = true,
            CustomText = "Downloading..."
        };

        // Assert
        Assert.Equal("download-progress", progressBar.Id);
        Assert.Equal(0.75f, progressBar.Value);
        Assert.Equal(0.0f, progressBar.Minimum);
        Assert.Equal(1.0f, progressBar.Maximum);
        Assert.True(progressBar.ShowPercentage);
        Assert.Equal("Downloading...", progressBar.CustomText);
    }

    [Fact]
    public void UIProgressBar_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var progressBar = new UIProgressBar();

        // Assert
        Assert.Equal(0.0f, progressBar.Value);
        Assert.Equal(0.0f, progressBar.Minimum);
        Assert.Equal(1.0f, progressBar.Maximum);
        Assert.True(progressBar.ShowPercentage);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void UIProgressBar_Value_AcceptsValidRange(float value)
    {
        // Arrange & Act
        var progressBar = new UIProgressBar { Value = value };

        // Assert
        Assert.Equal(value, progressBar.Value);
    }
}