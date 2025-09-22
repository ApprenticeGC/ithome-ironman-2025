using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;
using GameConsole.UI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.UI.Services;

/// <summary>
/// Console-based implementation of the UI service interface.
/// Provides TUI (Terminal User Interface) functionality using System.Console.
/// </summary>
[Service("ConsoleUIService")]
public class ConsoleUIService : GameConsole.UI.Core.IService
{
    private readonly ILogger<ConsoleUIService> _logger;
    private bool _isRunning;

    public ConsoleUIService(ILogger<ConsoleUIService> logger)
    {
        _logger = logger;
    }

    public bool IsRunning => _isRunning;

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing ConsoleUIService");
        
        // Setup console for UI operations
        Console.OutputEncoding = System.Text.Encoding.UTF8;
        
        _logger.LogInformation("Initialized ConsoleUIService");
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting ConsoleUIService");
        _isRunning = true;
        _logger.LogInformation("Started ConsoleUIService");
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping ConsoleUIService");
        _isRunning = false;
        _logger.LogInformation("Stopped ConsoleUIService");
        return Task.CompletedTask;
    }

    public Task DisplayMessageAsync(UIMessage message, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("ConsoleUIService is not running");

        var timestamp = message.Timestamp.ToString("HH:mm:ss");
        var prefix = GetMessagePrefix(message.Type);
        var formattedMessage = $"[{timestamp}] {prefix}: {message.Content}";

        // Apply color based on message type
        var originalColor = Console.ForegroundColor;
        try
        {
            Console.ForegroundColor = GetMessageColor(message.Type);
            Console.WriteLine(formattedMessage);
        }
        finally
        {
            Console.ForegroundColor = originalColor;
        }

        _logger.LogDebug("Displayed message: {MessageType} - {Content}", message.Type, message.Content);
        return Task.CompletedTask;
    }

    public async Task<string> DisplayMenuAsync(Menu menu, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("ConsoleUIService is not running");

        Console.WriteLine();
        Console.WriteLine($"=== {menu.Title} ===");
        
        for (int i = 0; i < menu.Items.Count; i++)
        {
            var item = menu.Items[i];
            var prefix = item.IsEnabled ? $"{i + 1}." : " x.";
            var display = item.IsEnabled ? item.Display : $"{item.Display} (disabled)";
            
            Console.ForegroundColor = item.IsEnabled ? ConsoleColor.White : ConsoleColor.DarkGray;
            Console.WriteLine($"{prefix} {display}");
            
            if (!string.IsNullOrEmpty(item.Description))
            {
                Console.ForegroundColor = ConsoleColor.Gray;
                Console.WriteLine($"   {item.Description}");
            }
        }
        
        Console.ForegroundColor = ConsoleColor.White;
        Console.WriteLine();

        while (true)
        {
            var input = await PromptInputAsync("Select option (number)", cancellationToken);
            
            if (int.TryParse(input, out int selection) && 
                selection >= 1 && selection <= menu.Items.Count)
            {
                var selectedItem = menu.Items[selection - 1];
                if (selectedItem.IsEnabled)
                {
                    _logger.LogDebug("Menu selection: {MenuTitle} - {ItemId}", menu.Title, selectedItem.Id);
                    return selectedItem.Id;
                }
                else
                {
                    await DisplayMessageAsync(new UIMessage("That option is disabled. Please select another.", MessageType.Warning), cancellationToken);
                }
            }
            else
            {
                await DisplayMessageAsync(new UIMessage("Invalid selection. Please enter a valid number.", MessageType.Error), cancellationToken);
            }
        }
    }

    public Task ClearDisplayAsync(CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("ConsoleUIService is not running");

        Console.Clear();
        _logger.LogDebug("Cleared console display");
        return Task.CompletedTask;
    }

    public Task<string> PromptInputAsync(string prompt, CancellationToken cancellationToken = default)
    {
        if (!_isRunning)
            throw new InvalidOperationException("ConsoleUIService is not running");

        Console.Write($"{prompt}: ");
        var input = Console.ReadLine() ?? string.Empty;
        
        _logger.LogDebug("User input received for prompt: {Prompt}", prompt);
        return Task.FromResult(input);
    }

    public ValueTask DisposeAsync()
    {
        if (_isRunning)
        {
            _ = StopAsync();
        }
        return ValueTask.CompletedTask;
    }

    private static string GetMessagePrefix(MessageType type)
    {
        return type switch
        {
            MessageType.Info => "INFO",
            MessageType.Warning => "WARN",
            MessageType.Error => "ERROR",
            MessageType.Success => "SUCCESS",
            _ => "INFO"
        };
    }

    private static ConsoleColor GetMessageColor(MessageType type)
    {
        return type switch
        {
            MessageType.Info => ConsoleColor.White,
            MessageType.Warning => ConsoleColor.Yellow,
            MessageType.Error => ConsoleColor.Red,
            MessageType.Success => ConsoleColor.Green,
            _ => ConsoleColor.White
        };
    }
}