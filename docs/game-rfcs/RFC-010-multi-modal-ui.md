# RFC-010: Multi-Modal UI System

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-002

## Summary

This RFC defines the multi-modal user interface system for GameConsole, enabling seamless switching between CLI (Command Line Interface) and TUI (Text User Interface) modes using System.CommandLine, Spectre.Console, and Terminal.Gui v2. The system provides consistent command routing and state management across different interaction modes.

### TUI Shell + Engine Simulation via Profiles

- The primary shell is TUI for both Game and Editor workflows; CLI remains available for scripting and batch tasks.
- Engine behaviors (Unity/Godot semantics) are simulated via Tier 3 profiles that swap behavior pipelines while keeping the UI in TUI.
- Core services and commands remain engine-agnostic; only the active profile changes which providers/systems are active.

## Motivation

GameConsole needs flexible UI interaction modes to support:

1. **CLI Mode**: Fast command execution for experienced users and scripting
2. **TUI Mode**: Interactive exploration and visual feedback for complex operations
3. **Seamless Switching**: Runtime transitions between CLI and TUI without losing context
4. **Command Consistency**: Same commands available in both modes
5. **Mode-Specific Optimizations**: Leverage strengths of each interface type
6. **Accessibility**: Support different user preferences and workflow styles

## Detailed Design

### UI Mode Architecture

```
Multi-Modal UI System
┌─────────────────────────────────────────────────────────────────┐
│ UI Mode Manager                                                 │
│ ├── Mode Detection & Switching                                  │
│ ├── Command Router                                              │
│ └── State Synchronization                                       │
└─────────────────────────────────────────────────────────────────┘
         │                                    │
         ▼                                    ▼
┌─────────────────────┐            ┌─────────────────────┐
│ CLI Mode            │            │ TUI Mode            │
│ ├── System.CommandLine │        │ ├── Terminal.Gui v2   │
│ ├── Spectre.Console    │        │ ├── Interactive Views │
│ ├── Direct Commands    │        │ ├── Navigation        │
│ └── Pipeline Support  │        │ └── Visual Feedback   │
└─────────────────────┐           └─────────────────────┘
                      │                     │
                      ▼                     ▼
               ┌─────────────────────────────────────┐
               │ Unified Command Processing Engine   │
               │ ├── Command Validation             │
               │ ├── Plugin Command Discovery       │
               │ ├── Context Management             │
               │ └── Result Formatting              │
               └─────────────────────────────────────┘
```

### Core UI Infrastructure

```csharp
// GameConsole.UI.Abstraction/src/IUIMode.cs
public interface IUIMode
{
    string Name { get; }
    bool IsInteractive { get; }
    UICapabilities SupportedCapabilities { get; }

    Task<bool> CanActivateAsync(UIContext context);
    Task ActivateAsync(UIContext context, CancellationToken cancellationToken);
    Task DeactivateAsync(CancellationToken cancellationToken);

    Task<CommandResult> ExecuteCommandAsync(ParsedCommand command, CancellationToken cancellationToken);
    Task ShowResultAsync(CommandResult result, CancellationToken cancellationToken);
}

[Flags]
public enum UICapabilities
{
    None = 0,
    TextInput = 1 << 0,
    FileSelection = 1 << 1,
    ProgressDisplay = 1 << 2,
    InteractiveNavigation = 1 << 3,
    RealTimeUpdates = 1 << 4,
    KeyboardShortcuts = 1 << 5,
    MouseInteraction = 1 << 6,
    ColorDisplay = 1 << 7,
    FormInput = 1 << 8,
    TableDisplay = 1 << 9
}

public record UIContext(
    string[] Args,
    Dictionary<string, object> State,
    ConsoleMode CurrentMode,
    UIPreferences Preferences);

public record ParsedCommand(
    string Name,
    Dictionary<string, object> Parameters,
    CommandMetadata Metadata);
```

### UI Mode Manager

```csharp
// GameConsole.UI.Core/src/UIModeManager.cs
public class UIModeManager : IUIModeManager
{
    private readonly IServiceRegistry<IUIMode> _modeRegistry;
    private readonly ICommandRouter _commandRouter;
    private readonly UIState _state;
    private readonly ILogger<UIModeManager> _logger;

    private IUIMode? _currentMode;

    public IUIMode? CurrentMode => _currentMode;
    public event EventHandler<UIModeChangedEventArgs>? ModeChanged;

    public UIModeManager(
        IServiceRegistry<IUIMode> modeRegistry,
        ICommandRouter commandRouter,
        UIState state,
        ILogger<UIModeManager> logger)
    {
        _modeRegistry = modeRegistry;
        _commandRouter = commandRouter;
        _state = state;
        _logger = logger;
    }

    public async Task<UILaunchResult> LaunchAsync(string[] args, CancellationToken cancellationToken)
    {
        var context = CreateUIContext(args);
        var targetMode = DetermineTargetMode(context);

        _logger.LogInformation("Launching UI in {Mode} mode", targetMode);

        try
        {
            await SwitchModeAsync(targetMode, context, cancellationToken);
            return new UILaunchResult(true, $"Launched in {targetMode} mode");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to launch UI in {Mode} mode", targetMode);
            return new UILaunchResult(false, ex.Message);
        }
    }

    public async Task SwitchModeAsync(string modeName, UIContext context, CancellationToken cancellationToken)
    {
        var newMode = _modeRegistry.GetProvider(new ProviderSelectionCriteria(
            RequiredCapabilities: new[] { modeName }.ToHashSet()));

        if (newMode == null)
        {
            throw new UIModeNotFoundException($"UI mode '{modeName}' not found");
        }

        if (!await newMode.CanActivateAsync(context))
        {
            throw new UIModeActivationException($"Cannot activate UI mode '{modeName}'");
        }

        // Deactivate current mode
        if (_currentMode != null)
        {
            _logger.LogDebug("Deactivating current UI mode: {Mode}", _currentMode.Name);
            await _currentMode.DeactivateAsync(cancellationToken);
        }

        // Activate new mode
        _logger.LogDebug("Activating UI mode: {Mode}", newMode.Name);
        await newMode.ActivateAsync(context, cancellationToken);

        var previousMode = _currentMode;
        _currentMode = newMode;

        // Notify mode change
        ModeChanged?.Invoke(this, new UIModeChangedEventArgs(previousMode, newMode));
        _logger.LogInformation("Switched UI mode from {Previous} to {Current}",
            previousMode?.Name ?? "None", newMode.Name);
    }

    private string DetermineTargetMode(UIContext context)
    {
        // Check for explicit mode specification
        if (context.Args.Contains("--cli"))
            return "CLI";
        if (context.Args.Contains("--tui"))
            return "TUI";

        // Auto-detect based on terminal capabilities and arguments
        if (HasInteractiveCommand(context.Args))
            return "TUI";

        if (Console.IsOutputRedirected || Console.IsInputRedirected)
            return "CLI";

        // Check terminal capabilities
        var terminalInfo = Environment.GetEnvironmentVariable("TERM");
        if (string.IsNullOrEmpty(terminalInfo) || terminalInfo == "dumb")
            return "CLI";

        // Default to CLI for single commands, TUI for interactive sessions
        return context.Args.Length > 0 ? "CLI" : "TUI";
    }

    private UIContext CreateUIContext(string[] args)
    {
        return new UIContext(
            Args: args,
            State: _state.GetCurrentState(),
            CurrentMode: _state.CurrentMode,
            Preferences: _state.Preferences);
    }
}
```

### CLI Mode Implementation

```csharp
// GameConsole.UI.CLI/src/CLIMode.cs
[ProviderFor(typeof(IUIMode))]
[Capability("CLI")]
[Capability("scripting")]
[Capability("pipeline")]
[Priority(50)]
public class CLIMode : IUIMode
{
    private readonly RootCommand _rootCommand;
    private readonly ICommandDiscovery _commandDiscovery;
    private readonly IAnsiConsole _console;
    private readonly ILogger<CLIMode> _logger;

    public string Name => "CLI";
    public bool IsInteractive => false;
    public UICapabilities SupportedCapabilities =>
        UICapabilities.TextInput |
        UICapabilities.ColorDisplay |
        UICapabilities.ProgressDisplay;

    public CLIMode(
        ICommandDiscovery commandDiscovery,
        IAnsiConsole console,
        ILogger<CLIMode> logger)
    {
        _commandDiscovery = commandDiscovery;
        _console = console;
        _logger = logger;
        _rootCommand = CreateRootCommand();
    }

    public async Task<bool> CanActivateAsync(UIContext context)
    {
        // CLI mode can always activate
        return await Task.FromResult(true);
    }

    public async Task ActivateAsync(UIContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Activating CLI mode");

        // Register all discovered commands
        await RegisterCommandsAsync(cancellationToken);

        // If arguments provided, execute immediately
        if (context.Args.Length > 0)
        {
            await _rootCommand.InvokeAsync(context.Args);
        }
        else
        {
            // Show help for interactive CLI usage
            _console.WriteLine("GameConsole CLI Mode");
            _console.WriteLine("Use 'help' for available commands or '--tui' to switch to interactive mode.");
        }
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deactivating CLI mode");
        // CLI mode cleanup if needed
        await Task.CompletedTask;
    }

    public async Task<CommandResult> ExecuteCommandAsync(ParsedCommand command, CancellationToken cancellationToken)
    {
        try
        {
            var result = await _rootCommand.InvokeAsync(BuildCommandLine(command));
            return new CommandResult(result == 0, result == 0 ? "Command completed successfully" : "Command failed");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed: {Command}", command.Name);
            return new CommandResult(false, ex.Message);
        }
    }

    public async Task ShowResultAsync(CommandResult result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            if (!string.IsNullOrEmpty(result.Output))
            {
                _console.WriteLine(result.Output);
            }
        }
        else
        {
            _console.MarkupLine($"[red]Error:[/] {result.ErrorMessage}");
        }

        await Task.CompletedTask;
    }

    private RootCommand CreateRootCommand()
    {
        var root = new RootCommand("GameConsole - Plugin-centric game development toolkit");

        // Add global options
        root.AddGlobalOption(new Option<bool>("--tui", "Switch to TUI mode"));
        root.AddGlobalOption(new Option<bool>("--verbose", "Enable verbose output"));

        // Built-in commands
        root.AddCommand(CreatePluginCommand());
        root.AddCommand(CreateModeCommand());
        root.AddCommand(CreateConfigCommand());

        return root;
    }

    private Command CreatePluginCommand()
    {
        var pluginCmd = new Command("plugin", "Plugin management commands");

        var listCmd = new Command("list", "List installed plugins");
        listCmd.SetHandler(async () =>
        {
            var plugins = await _commandDiscovery.GetAvailablePluginsAsync();
            var table = new Table()
                .AddColumn("Name")
                .AddColumn("Version")
                .AddColumn("Status")
                .AddColumn("Description");

            foreach (var plugin in plugins)
            {
                table.AddRow(
                    plugin.Name,
                    plugin.Version.ToString(),
                    plugin.IsLoaded ? "[green]Loaded[/]" : "[red]Not Loaded[/]",
                    plugin.Description);
            }

            _console.Write(table);
        });

        var loadCmd = new Command("load", "Load a plugin");
        var nameArg = new Argument<string>("name", "Plugin name to load");
        loadCmd.AddArgument(nameArg);
        loadCmd.SetHandler(async (string name) =>
        {
            _console.Status()
                .Start($"Loading plugin {name}...", async ctx =>
                {
                    try
                    {
                        await _commandDiscovery.LoadPluginAsync(name);
                        ctx.Status($"[green]Successfully loaded plugin {name}[/]");
                    }
                    catch (Exception ex)
                    {
                        ctx.Status($"[red]Failed to load plugin {name}: {ex.Message}[/]");
                    }
                });
        }, nameArg);

        pluginCmd.AddCommand(listCmd);
        pluginCmd.AddCommand(loadCmd);

        return pluginCmd;
    }

    private async Task RegisterCommandsAsync(CancellationToken cancellationToken)
    {
        var discoveredCommands = await _commandDiscovery.DiscoverCommandsAsync(cancellationToken);

        foreach (var cmdInfo in discoveredCommands)
        {
            var command = CreateCommandFromInfo(cmdInfo);
            _rootCommand.AddCommand(command);
        }
    }
}
```

### TUI Mode Implementation

```csharp
// GameConsole.UI.TUI/src/TUIMode.cs
[ProviderFor(typeof(IUIMode))]
[Capability("TUI")]
[Capability("interactive")]
[Capability("navigation")]
[Priority(60)]
public class TUIMode : IUIMode
{
    private readonly ICommandDiscovery _commandDiscovery;
    private readonly ILogger<TUIMode> _logger;

    private Application? _application;
    private MainWindow? _mainWindow;
    private CancellationTokenSource? _uiCancellation;

    public string Name => "TUI";
    public bool IsInteractive => true;
    public UICapabilities SupportedCapabilities =>
        UICapabilities.TextInput |
        UICapabilities.FileSelection |
        UICapabilities.ProgressDisplay |
        UICapabilities.InteractiveNavigation |
        UICapabilities.RealTimeUpdates |
        UICapabilities.KeyboardShortcuts |
        UICapabilities.MouseInteraction |
        UICapabilities.ColorDisplay |
        UICapabilities.FormInput |
        UICapabilities.TableDisplay;

    public async Task<bool> CanActivateAsync(UIContext context)
    {
        // Check if terminal supports TUI
        try
        {
            Application.Init();
            Application.Shutdown();
            return await Task.FromResult(true);
        }
        catch
        {
            return await Task.FromResult(false);
        }
    }

    public async Task ActivateAsync(UIContext context, CancellationToken cancellationToken)
    {
        _logger.LogDebug("Activating TUI mode");

        _uiCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Initialize Terminal.Gui
        Application.Init();

        try
        {
            // Create main application window
            _mainWindow = new MainWindow(_commandDiscovery, _logger);
            _application = new Application();

            // Setup global key bindings
            Application.Top.KeyPress += HandleGlobalKeyPress;

            // Run the application
            Application.Run(_mainWindow);
        }
        finally
        {
            Application.Shutdown();
        }
    }

    public async Task DeactivateAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Deactivating TUI mode");

        _uiCancellation?.Cancel();

        if (_application != null)
        {
            Application.RequestStop();
            _application = null;
        }

        _mainWindow?.Dispose();
        _mainWindow = null;

        await Task.CompletedTask;
    }

    public async Task<CommandResult> ExecuteCommandAsync(ParsedCommand command, CancellationToken cancellationToken)
    {
        if (_mainWindow == null)
            throw new InvalidOperationException("TUI not initialized");

        return await _mainWindow.ExecuteCommandAsync(command, cancellationToken);
    }

    public async Task ShowResultAsync(CommandResult result, CancellationToken cancellationToken)
    {
        if (_mainWindow == null)
            throw new InvalidOperationException("TUI not initialized");

        await _mainWindow.ShowResultAsync(result, cancellationToken);
    }

    private void HandleGlobalKeyPress(KeyEventEventArgs e)
    {
        // Handle global shortcuts
        if (e.KeyEvent.Key == Key.F1)
        {
            _mainWindow?.ShowHelp();
            e.Handled = true;
        }
        else if (e.KeyEvent.Key == (Key.Q | Key.CtrlMask))
        {
            // Quit application
            Application.RequestStop();
            e.Handled = true;
        }
        else if (e.KeyEvent.Key == (Key.D | Key.CtrlMask))
        {
            // Switch to CLI mode
            _mainWindow?.SwitchToCLI();
            e.Handled = true;
        }
    }
}

// GameConsole.UI.TUI/src/MainWindow.cs
public class MainWindow : Window
{
    private readonly ICommandDiscovery _commandDiscovery;
    private readonly ILogger _logger;

    private MenuBar _menuBar;
    private StatusBar _statusBar;
    private FrameView _contentFrame;
    private CommandPalette _commandPalette;
    private OutputView _outputView;
    private PluginExplorer _pluginExplorer;

    public MainWindow(ICommandDiscovery commandDiscovery, ILogger logger) : base("GameConsole")
    {
        _commandDiscovery = commandDiscovery;
        _logger = logger;

        InitializeComponents();
        SetupLayout();
        SetupKeyBindings();
    }

    private void InitializeComponents()
    {
        // Menu bar
        _menuBar = new MenuBar(new MenuBarItem[]
        {
            new MenuBarItem("_File", new MenuItem[]
            {
                new MenuItem("_New Project", "", NewProject),
                new MenuItem("_Open Project", "", OpenProject),
                null, // Separator
                new MenuItem("_Exit", "", () => Application.RequestStop())
            }),
            new MenuBarItem("_Plugins", new MenuItem[]
            {
                new MenuItem("_Browse", "", ShowPluginExplorer),
                new MenuItem("_Reload All", "", ReloadPlugins)
            }),
            new MenuBarItem("_View", new MenuItem[]
            {
                new MenuItem("_Command Palette", "Ctrl+Shift+P", ShowCommandPalette),
                new MenuItem("_Output", "Ctrl+`", ShowOutput),
                null,
                new MenuItem("Switch to _CLI", "Ctrl+D", SwitchToCLI)
            }),
            new MenuBarItem("_Help", new MenuItem[]
            {
                new MenuItem("_About", "", ShowAbout),
                new MenuItem("_Documentation", "F1", ShowHelp)
            })
        });

        // Status bar
        _statusBar = new StatusBar(new StatusItem[]
        {
            new StatusItem(Key.F1, "~F1~ Help", ShowHelp),
            new StatusItem(Key.F10, "~F10~ Menu", () => _menuBar.OpenMenu()),
            new StatusItem(Key.CtrlMask | Key.Q, "~^Q~ Quit", () => Application.RequestStop())
        });

        // Content frame
        _contentFrame = new FrameView("Main")
        {
            X = 0,
            Y = 1,
            Width = Dim.Fill(),
            Height = Dim.Fill(1)
        };

        // Command palette (initially hidden)
        _commandPalette = new CommandPalette(_commandDiscovery)
        {
            X = Pos.Center(),
            Y = Pos.Center(),
            Width = 80,
            Height = 20,
            Visible = false
        };

        // Output view
        _outputView = new OutputView()
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };

        // Plugin explorer
        _pluginExplorer = new PluginExplorer(_commandDiscovery)
        {
            X = 0,
            Y = 0,
            Width = Dim.Fill(),
            Height = Dim.Fill()
        };
    }

    private void SetupLayout()
    {
        Add(_menuBar);
        Add(_contentFrame);
        Add(_statusBar);
        Add(_commandPalette);

        // Start with output view
        _contentFrame.Add(_outputView);

        // Set focus order
        this.CanFocus = true;
    }

    private void SetupKeyBindings()
    {
        // Command palette
        Application.Top.KeyPress += (e) =>
        {
            if (e.KeyEvent.Key == (Key.P | Key.CtrlMask | Key.ShiftMask))
            {
                ShowCommandPalette();
                e.Handled = true;
            }
        };
    }

    public async Task<CommandResult> ExecuteCommandAsync(ParsedCommand command, CancellationToken cancellationToken)
    {
        _outputView.AppendLine($"Executing: {command.Name}");

        try
        {
            // Execute command through discovery service
            var result = await _commandDiscovery.ExecuteCommandAsync(command.Name, command.Parameters, cancellationToken);
            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed: {Command}", command.Name);
            return new CommandResult(false, ex.Message);
        }
    }

    public async Task ShowResultAsync(CommandResult result, CancellationToken cancellationToken)
    {
        if (result.IsSuccess)
        {
            _outputView.AppendLine($"[SUCCESS] {result.Output}", ConsoleColor.Green);
        }
        else
        {
            _outputView.AppendLine($"[ERROR] {result.ErrorMessage}", ConsoleColor.Red);
        }

        await Task.CompletedTask;
    }

    public void SwitchToCLI()
    {
        // Request mode switch to CLI
        Application.RequestStop();
        // The mode manager will handle the actual switch
    }

    // Event handlers
    private void ShowCommandPalette()
    {
        _commandPalette.Visible = true;
        _commandPalette.SetFocus();
    }

    private void ShowPluginExplorer()
    {
        _contentFrame.RemoveAll();
        _contentFrame.Add(_pluginExplorer);
        _pluginExplorer.Refresh();
    }

    private void ShowOutput()
    {
        _contentFrame.RemoveAll();
        _contentFrame.Add(_outputView);
    }

    public void ShowHelp()
    {
        MessageBox.Query("Help", "GameConsole TUI\n\nF1: Help\nCtrl+Shift+P: Command Palette\nCtrl+D: Switch to CLI\nCtrl+Q: Quit", "OK");
    }

    private void ShowAbout()
    {
        MessageBox.Query("About", "GameConsole v1.0\nPlugin-centric game development toolkit", "OK");
    }
}
```

### Command Integration

```csharp
// GameConsole.UI.Commands/src/CommandRouter.cs
public class CommandRouter : ICommandRouter
{
    private readonly IServiceRegistry<ICommandHandler> _handlerRegistry;
    private readonly ICommandValidator _validator;
    private readonly ILogger<CommandRouter> _logger;

    public async Task<CommandResult> RouteCommandAsync(
        ParsedCommand command,
        UIContext context,
        CancellationToken cancellationToken)
    {
        // Validate command
        var validationResult = await _validator.ValidateAsync(command, context, cancellationToken);
        if (!validationResult.IsValid)
        {
            return new CommandResult(false, validationResult.ErrorMessage);
        }

        // Find appropriate handler
        var handler = _handlerRegistry.GetProvider(new ProviderSelectionCriteria(
            RequiredCapabilities: new[] { command.Name }.ToHashSet()));

        if (handler == null)
        {
            return new CommandResult(false, $"No handler found for command: {command.Name}");
        }

        // Execute command
        try
        {
            return await handler.ExecuteAsync(command, context, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Command execution failed: {Command}", command.Name);
            return new CommandResult(false, ex.Message);
        }
    }
}
```

## Benefits

### User Experience
- Flexible interaction modes for different user preferences
- Seamless switching between CLI and TUI without losing context
- Consistent command interface across modes

### Developer Productivity
- CLI mode for scripting and automation
- TUI mode for interactive exploration and complex operations
- Visual feedback and navigation in TUI mode

### Accessibility
- Support for different terminal capabilities
- Keyboard navigation and shortcuts
- Screen reader compatibility through proper terminal usage

### Maintainability
- Unified command processing engine
- Clear separation between UI modes and business logic
- Plugin commands automatically available in both modes

## Drawbacks

### Complexity
- Multiple UI frameworks to maintain
- Complex mode switching logic
- State synchronization between modes

### Resource Usage
- Both UI frameworks loaded in memory
- Overhead of mode detection and switching
- Terminal capability detection complexity

### Development Overhead
- Commands must work in both CLI and TUI contexts
- Testing across different terminal environments
- UI-specific optimizations and layouts

## Alternatives Considered

### Single Mode Approach
- Simpler but limits user flexibility
- **Rejected**: Different users have different preferences

### Web-Based UI
- More features but requires browser and web server
- **Rejected**: Adds complexity and deployment requirements

### Immediate Mode GUI (Dear ImGui)
- Rich UI capabilities but requires graphics context
- **Rejected**: Doesn't work in terminal environments

## Migration Strategy

### Phase 1: Basic Mode Infrastructure
- Implement IUIMode interface and mode manager
- Create basic CLI mode with System.CommandLine
- Add mode detection and switching logic

### Phase 2: TUI Implementation
- Implement TUI mode with Terminal.Gui v2
- Add interactive components (menus, dialogs, navigation)
- Create visual command palette and output display

### Phase 3: Command Integration
- Implement unified command router
- Add plugin command discovery
- Create consistent command execution across modes

### Phase 4: Advanced Features
- Add advanced TUI layouts and components
- Implement context-aware help system
- Add keyboard shortcuts and accessibility features

## Success Metrics

- **Mode Switching**: Sub-second transitions between CLI and TUI
- **Command Consistency**: 100% command compatibility across modes
- **User Satisfaction**: Preference-based mode selection
- **Performance**: No noticeable overhead from multi-modal support

## Future Possibilities

- **Web UI Mode**: Browser-based interface for remote access
- **Voice Commands**: Speech recognition for accessibility
- **Custom UI Modes**: Plugin-defined interface modes
- **Multi-Terminal Support**: Simultaneous CLI and TUI instances
