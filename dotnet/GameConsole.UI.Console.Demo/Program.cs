using GameConsole.UI.Console;

// Demo data class for table
public class DemoTask
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "";
    public int Progress { get; set; }
    public DateTime CreatedDate { get; set; }
}

class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("🎮 GameConsole UI Framework Demo - GAME-RFC-010-02");
        Console.WriteLine("This demo showcases the rich console UI components with ANSI formatting.");
        Console.WriteLine("Press any key to start the demo...");
        Console.ReadKey();
        
        var framework = new ConsoleUIFramework();
        
        try
        {
            await framework.InitializeAsync();
            await framework.StartAsync();
            
            await RunDemos(framework);
        }
        finally
        {
            await framework.StopAsync();
        }
        
        Console.WriteLine("\n✅ Demo completed successfully!");
        Console.WriteLine("The GameConsole.UI.Console implementation provides:");
        Console.WriteLine("  ✓ Rich console components with color and formatting");
        Console.WriteLine("  ✓ Interactive menus with keyboard navigation");
        Console.WriteLine("  ✓ Data tables with sorting and filtering capabilities");
        Console.WriteLine("  ✓ Progress indicators and status displays");
        Console.WriteLine("  ✓ Multi-pane layout support");
        Console.WriteLine("  ✓ Responsive console layouts for different terminal sizes");
        Console.WriteLine("  ✓ Unicode and emoji support");
    }
    
    static async Task RunDemos(ConsoleUIFramework framework)
    {
        // Demo 1: Menu Component
        await DemoMenu(framework);
        
        // Demo 2: Progress Bar Component
        await DemoProgressBars(framework);
        
        // Demo 3: Table Component  
        await DemoTable(framework);
        
        // Demo 4: Layout System
        await DemoLayouts(framework);
        
        // Demo 5: Message Boxes
        await DemoMessageBoxes(framework);
    }
    
    static async Task DemoMenu(ConsoleUIFramework framework)
    {
        await framework.ShowMessageBoxAsync(
            "📋 MENU COMPONENT DEMO\n\nThis demonstrates interactive console menus with:\n• Keyboard navigation (↑↓ arrows, Enter)\n• Hotkey support\n• Visual selection indicators\n• Color formatting",
            "Menu Demo");
        
        var menu = framework.CreateMenu("🎮 Game Console Menu");
        menu.AddItem("🎯 New Game", () => Console.WriteLine("New Game selected!"), 'n');
        menu.AddItem("💾 Load Game", () => Console.WriteLine("Load Game selected!"), 'l');
        menu.AddItem("⚙️ Settings", () => Console.WriteLine("Settings selected!"), 's');
        menu.AddSeparator("── Advanced ──");
        menu.AddItem("🧪 Debug Mode", () => Console.WriteLine("Debug Mode selected!"), 'd');
        menu.AddItem("📊 Statistics", () => Console.WriteLine("Statistics selected!"), 't');
        menu.AddSeparator();
        menu.AddItem("❌ Exit", () => Console.WriteLine("Exit selected!"), 'x');
        
        framework.Render(menu);
        
        // Simulate some menu interactions
        await Task.Delay(1000);
        menu.MoveToNext();
        framework.Render(menu);
        
        await Task.Delay(800);
        menu.MoveToNext();
        framework.Render(menu);
        
        await Task.Delay(800);
        Console.WriteLine("\n🎯 Menu navigation demo completed!");
        await Task.Delay(2000);
    }
    
    static async Task DemoProgressBars(ConsoleUIFramework framework)
    {
        await framework.ShowMessageBoxAsync(
            "📊 PROGRESS BAR DEMO\n\nThis demonstrates progress indicators with:\n• Multiple visual styles\n• Percentage display\n• Smooth animation\n• Color customization",
            "Progress Demo");
        
        var multiProgress = framework.CreateMultiProgressBar("🔄 System Loading Progress");
        
        var gameProgress = multiProgress.AddProgressBar("Game Engine", ProgressBarStyle.Block, 
            ANSIEscapeSequences.FgBrightGreen, ANSIEscapeSequences.FgBrightBlack);
        var audioProgress = multiProgress.AddProgressBar("Audio System", ProgressBarStyle.Shaded,
            ANSIEscapeSequences.FgBrightBlue, ANSIEscapeSequences.FgBrightBlack);
        var uiProgress = multiProgress.AddProgressBar("UI Framework", ProgressBarStyle.Hash,
            ANSIEscapeSequences.FgBrightYellow, ANSIEscapeSequences.FgBrightBlack);
        
        // Animate the progress bars
        for (int i = 0; i <= 100; i += 5)
        {
            gameProgress.SetPercentage(Math.Min(100, i * 1.2));
            audioProgress.SetPercentage(Math.Min(100, i * 0.8));
            uiProgress.SetPercentage(Math.Min(100, i * 1.5));
            
            framework.Render(multiProgress);
            await Task.Delay(150);
        }
        
        Console.WriteLine("\n🎉 All systems loaded successfully!");
        await Task.Delay(2000);
    }
    
    static async Task DemoTable(ConsoleUIFramework framework)
    {
        await framework.ShowMessageBoxAsync(
            "📋 TABLE COMPONENT DEMO\n\nThis demonstrates data tables with:\n• Sortable columns\n• Border formatting\n• Column alignment\n• Data binding",
            "Table Demo");
        
        var table = framework.CreateTable<DemoTask>("📋 Task Management Dashboard");
        table.AddColumn("Task Name", nameof(DemoTask.Name), 25, LayoutAlignment.Start);
        table.AddColumn("Status", nameof(DemoTask.Status), 12, LayoutAlignment.Center);
        table.AddColumn("Progress", nameof(DemoTask.Progress), 10, LayoutAlignment.End);
        table.AddColumn("Created", nameof(DemoTask.CreatedDate), 12, LayoutAlignment.Start);
        
        // Set custom formatter for progress column
        table.Columns.First(c => c.PropertyName == nameof(DemoTask.Progress))
            .Formatter = obj => $"{obj}%";
        
        // Set custom formatter for date column
        table.Columns.First(c => c.PropertyName == nameof(DemoTask.CreatedDate))
            .Formatter = obj => obj is DateTime dt ? dt.ToString("MM/dd/yyyy") : "";
        
        var tasks = new List<DemoTask>
        {
            new() { Name = "🎮 Implement Game Logic", Status = "✅ Complete", Progress = 100, CreatedDate = DateTime.Now.AddDays(-5) },
            new() { Name = "🎨 Design UI Components", Status = "🟡 In Progress", Progress = 75, CreatedDate = DateTime.Now.AddDays(-3) },
            new() { Name = "🔊 Add Audio System", Status = "🔴 Pending", Progress = 25, CreatedDate = DateTime.Now.AddDays(-2) },
            new() { Name = "🧪 Write Unit Tests", Status = "🟡 In Progress", Progress = 60, CreatedDate = DateTime.Now.AddDays(-1) },
            new() { Name = "📚 Update Documentation", Status = "🔴 Pending", Progress = 10, CreatedDate = DateTime.Now }
        };
        
        table.SetData(tasks);
        framework.Render(table);
        
        await Task.Delay(2000);
        
        // Demonstrate sorting
        Console.WriteLine("\n🔄 Sorting by Progress (descending)...");
        await Task.Delay(1000);
        table.SortBy(nameof(DemoTask.Progress), false);
        framework.Render(table);
        
        await Task.Delay(2000);
        Console.WriteLine("\n📊 Table demo completed!");
    }
    
    static async Task DemoLayouts(ConsoleUIFramework framework)
    {
        await framework.ShowMessageBoxAsync(
            "🏗️ LAYOUT SYSTEM DEMO\n\nThis demonstrates layout management with:\n• Multi-pane layouts\n• Responsive design\n• Border elements\n• Flexible positioning",
            "Layout Demo");
        
        var layoutManager = framework.LayoutManager;
        
        // Create a complex layout
        var mainContainer = layoutManager.CreateVerticalContainer(spacing: 1, padding: new LayoutSpacing(2));
        
        // Header
        var header = layoutManager.CreateTextElement("🎮 GameConsole UI Framework", LayoutAlignment.Center, 
            ANSIEscapeSequences.FgBrightWhite + ANSIEscapeSequences.Bold);
        var headerBorder = layoutManager.CreateBorderElement(header, "Header", 
            ANSIEscapeSequences.FgBrightBlue);
        mainContainer.AddChild(headerBorder);
        
        // Content area with horizontal layout
        var contentContainer = layoutManager.CreateHorizontalContainer(spacing: 2);
        
        // Left panel
        var leftPanel = layoutManager.CreateVerticalContainer(spacing: 1);
        leftPanel.AddChild(layoutManager.CreateTextElement("📊 Statistics", LayoutAlignment.Center, ANSIEscapeSequences.FgBrightGreen));
        leftPanel.AddChild(layoutManager.CreateTextElement("• Active Users: 1,234", LayoutAlignment.Start));
        leftPanel.AddChild(layoutManager.CreateTextElement("• Games Played: 5,678", LayoutAlignment.Start));
        leftPanel.AddChild(layoutManager.CreateTextElement("• Uptime: 99.9%", LayoutAlignment.Start));
        var leftBorder = layoutManager.CreateBorderElement(leftPanel, "Stats", ANSIEscapeSequences.FgBrightGreen);
        contentContainer.AddChild(leftBorder);
        
        // Right panel
        var rightPanel = layoutManager.CreateVerticalContainer(spacing: 1);
        rightPanel.AddChild(layoutManager.CreateTextElement("🔧 System Status", LayoutAlignment.Center, ANSIEscapeSequences.FgBrightYellow));
        rightPanel.AddChild(layoutManager.CreateTextElement("• CPU Usage: 45%", LayoutAlignment.Start));
        rightPanel.AddChild(layoutManager.CreateTextElement("• Memory: 2.1GB/8GB", LayoutAlignment.Start));
        rightPanel.AddChild(layoutManager.CreateTextElement("• Network: Active", LayoutAlignment.Start));
        var rightBorder = layoutManager.CreateBorderElement(rightPanel, "System", ANSIEscapeSequences.FgBrightYellow);
        contentContainer.AddChild(rightBorder);
        
        mainContainer.AddChild(contentContainer);
        
        // Footer
        var footer = layoutManager.CreateTextElement("Press any key to continue...", LayoutAlignment.Center, 
            ANSIEscapeSequences.FgBrightBlack);
        var footerBorder = layoutManager.CreateBorderElement(footer, "Footer", 
            ANSIEscapeSequences.FgBrightBlack);
        mainContainer.AddChild(footerBorder);
        
        framework.RootContainer = mainContainer;
        framework.Render();
        
        await Task.Delay(3000);
        Console.WriteLine("\n🏗️ Multi-pane layout demo completed!");
    }
    
    static async Task DemoMessageBoxes(ConsoleUIFramework framework)
    {
        await framework.ShowMessageBoxAsync(
            "💬 DIALOG SYSTEM DEMO\n\nThis demonstrates interactive dialogs with:\n• Message boxes\n• Input dialogs\n• Modal overlays\n• Keyboard interaction",
            "Dialog Demo");
        
        // Show a confirmation-style message
        await framework.ShowMessageBoxAsync(
            "⚠️  This is a warning message!\n\nThis demonstrates how message boxes can display important information to users with proper formatting and visual hierarchy.",
            "⚠️ Warning");
        
        // Show an input dialog
        var result = await framework.ShowInputDialogAsync(
            "Please enter your name:", "🙋 User Input");
        
        if (!string.IsNullOrEmpty(result))
        {
            await framework.ShowMessageBoxAsync(
                $"Hello, {result}! 👋\n\nYour input was successfully captured and processed.",
                "✅ Success");
        }
        
        Console.WriteLine("\n💬 Dialog system demo completed!");
    }
}
