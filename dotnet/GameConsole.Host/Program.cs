using GameConsole.Engine.Core;
using GameConsole.Plugins.Core;

namespace GameConsole.Host;

/// <summary>
/// Main host application for GameConsole system.
/// Demonstrates the 4-tier service architecture with container-native deployment.
/// </summary>
public class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("GameConsole Host Starting...");
        Console.WriteLine($"Version: {GetVersion()}");
        Console.WriteLine($"Environment: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development"}");
        
        // Initialize basic game console components
        var serviceProvider = new GameConsole.Core.Registry.ServiceProvider();
        
        Console.WriteLine("GameConsole Host initialized successfully");
        Console.WriteLine("Press Ctrl+C to exit...");
        
        // Keep the application running
        var cancellationTokenSource = new CancellationTokenSource();
        Console.CancelKeyPress += (_, e) =>
        {
            e.Cancel = true;
            cancellationTokenSource.Cancel();
            Console.WriteLine("\nShutting down GameConsole Host...");
        };
        
        try
        {
            await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        
        Console.WriteLine("GameConsole Host stopped.");
    }
    
    private static string GetVersion()
    {
        var assembly = System.Reflection.Assembly.GetExecutingAssembly();
        var version = assembly.GetName().Version;
        return version?.ToString() ?? "Unknown";
    }
}