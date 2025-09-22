using GameConsole.UI.Console;

namespace GameConsole.UI.Console.Tests;

public class IntegrationTests
{
    [Fact]
    public void ConsoleUIFramework_CanInitializeAndCreateComponents()
    {
        var framework = new ConsoleUIFramework();
        
        // Test service lifecycle
        Assert.False(framework.IsRunning);
        
        // Test component creation methods
        var menu = framework.CreateMenu("Test Menu");
        Assert.NotNull(menu);
        
        var table = framework.CreateTable<TestData>("Test Table");
        Assert.NotNull(table);
        
        var progressBar = framework.CreateProgressBar("Test Progress");
        Assert.NotNull(progressBar);
        
        var multiProgress = framework.CreateMultiProgressBar("Multi Progress");
        Assert.NotNull(multiProgress);
        
        // Test terminal info
        Assert.NotNull(framework.TerminalInfo);
        Assert.NotNull(framework.LayoutManager);
        Assert.NotNull(framework.InputManager);
    }
    
    [Fact]
    public void FullLayoutRenderingWorkflow_ExecutesWithoutErrors()
    {
        var framework = new ConsoleUIFramework();
        var layoutManager = framework.LayoutManager;
        
        // Create a complex layout similar to the demo
        var container = layoutManager.CreateVerticalContainer(spacing: 1);
        
        var header = layoutManager.CreateTextElement("Test Header", LayoutAlignment.Center);
        var borderHeader = layoutManager.CreateBorderElement(header, "Header");
        container.AddChild(borderHeader);
        
        var progressBar = framework.CreateProgressBar("Loading");
        progressBar.Value = 0.75;
        container.AddChild(progressBar);
        
        var menu = framework.CreateMenu("Test Menu");
        menu.AddItem("Option 1");
        menu.AddItem("Option 2");
        container.AddChild(menu);
        
        // Test rendering to buffer
        var buffer = layoutManager.RenderToBuffer(container);
        Assert.NotNull(buffer);
        Assert.True(buffer.Width > 0);
        Assert.True(buffer.Height > 0);
        
        // Test buffer rendering to string
        var output = buffer.Render();
        Assert.NotNull(output);
        Assert.Contains("Test Header", output);
        Assert.Contains("Loading", output);
        Assert.Contains("Option 1", output);
    }
    
    [Fact]
    public void TableWithRealData_RendersCorrectly()
    {
        var table = new ConsoleTable<TestData>("Test Data");
        table.AddColumn("ID", nameof(TestData.Id), 5);
        table.AddColumn("Name", nameof(TestData.Name), 15);
        table.AddColumn("Value", nameof(TestData.Value), 10);
        
        var testData = new List<TestData>
        {
            new() { Id = 1, Name = "First Item", Value = 100.5 },
            new() { Id = 2, Name = "Second Item", Value = 200.75 },
            new() { Id = 3, Name = "Third Item", Value = 50.25 }
        };
        
        table.SetData(testData);
        
        var buffer = new ConsoleBuffer(50, 20);
        var bounds = new ConsoleRect(0, 0, 50, 20);
        
        // Should not throw
        table.Render(buffer, bounds);
        
        var output = buffer.Render();
        Assert.Contains("Test Data", output);
        Assert.Contains("First Item", output);
        Assert.Contains("Second Item", output);
        Assert.Contains("Third Item", output);
    }
    
    [Fact]
    public void MenuKeyboardNavigation_WorksCorrectly()
    {
        var menu = new ConsoleMenu("Navigation Test");
        menu.AddItem("Item 1", null, '1');
        menu.AddItem("Item 2", null, '2');
        menu.AddSeparator();
        menu.AddItem("Item 3", null, '3');
        
        Assert.Equal(0, menu.SelectedIndex);
        
        // Test navigation
        menu.MoveToNext();
        Assert.Equal(1, menu.SelectedIndex);
        
        menu.MoveToNext(); // Should skip separator
        Assert.Equal(3, menu.SelectedIndex);
        
        menu.MoveToPrevious();
        Assert.Equal(1, menu.SelectedIndex);
        
        // Test hotkey handling
        var handled = menu.HandleKeyInput(GameConsole.Input.Core.KeyCode.Alpha3);
        Assert.True(handled);
        Assert.Equal(3, menu.SelectedIndex);
    }
    
    [Fact]
    public void ComplexLayoutWithAllComponents_RendersSuccessfully()
    {
        var framework = new ConsoleUIFramework();
        var layoutManager = framework.LayoutManager;
        
        // Create a layout that uses all major components
        var mainContainer = layoutManager.CreateVerticalContainer(spacing: 1);
        
        // Header with text
        var headerText = layoutManager.CreateTextElement("🎮 Console UI Test", LayoutAlignment.Center);
        var header = layoutManager.CreateBorderElement(headerText, "Header");
        mainContainer.AddChild(header);
        
        // Horizontal container for side-by-side content
        var contentContainer = layoutManager.CreateHorizontalContainer(spacing: 2);
        
        // Left side: Menu
        var menu = framework.CreateMenu("Options");
        menu.AddItem("Start Game");
        menu.AddItem("Settings");
        menu.AddItem("Exit");
        contentContainer.AddChild(menu);
        
        // Right side: Progress and status
        var rightSide = layoutManager.CreateVerticalContainer();
        var progressBar = framework.CreateProgressBar("Loading", ProgressBarStyle.Block);
        progressBar.SetPercentage(85);
        rightSide.AddChild(progressBar);
        
        var statusText = layoutManager.CreateTextElement("System Ready", LayoutAlignment.Center);
        rightSide.AddChild(statusText);
        
        contentContainer.AddChild(rightSide);
        mainContainer.AddChild(contentContainer);
        
        // Render everything
        var buffer = layoutManager.RenderToBuffer(mainContainer);
        var output = buffer.Render();
        
        // Verify key elements are present
        Assert.Contains("Console UI Test", output);
        Assert.Contains("Start Ga", output); // May be truncated in layout
        Assert.Contains("Loading", output);
        Assert.Contains("System Ready", output);
        Assert.Contains("█", output); // Progress bar character
        Assert.Contains("│", output); // Border character
    }
    
    private class TestData
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public double Value { get; set; }
    }
}