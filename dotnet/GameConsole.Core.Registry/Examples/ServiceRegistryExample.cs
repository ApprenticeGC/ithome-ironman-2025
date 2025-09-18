using GameConsole.Core.Abstractions;
using GameConsole.Core.Registry;

namespace GameConsole.Core.Registry.Examples;

/// <summary>
/// Example demonstrating the Service Registry Pattern usage.
/// </summary>
public static class ServiceRegistryExample
{
    /// <summary>
    /// Demonstrates basic service registration and resolution.
    /// </summary>
    public static async Task BasicUsageExample()
    {
        // Create service provider
        using var serviceProvider = new ServiceProvider();

        // Register services programmatically
        serviceProvider.RegisterSingleton<ILogger, ConsoleLogger>();
        serviceProvider.RegisterTransient<IMessageService, MessageService>();
        serviceProvider.RegisterScoped<IDataService, DataService>();

        // Resolve and use services
        var messageService = await serviceProvider.GetServiceAsync<IMessageService>();
        messageService?.SendMessage("Hello from Service Registry!");

        var logger = serviceProvider.GetService<ILogger>();
        logger?.Log("Service registry working correctly!");
    }

    /// <summary>
    /// Demonstrates attribute-based service registration.
    /// </summary>
    public static void AttributeBasedRegistrationExample()
    {
        using var serviceProvider = new ServiceProvider();

        // Register services from attributes in current assembly
        serviceProvider.RegisterFromAttributes(typeof(ServiceRegistryExample).Assembly);

        // Services with [Service] attributes are now available
        var attributeService = serviceProvider.GetService<IAttributeService>();
        attributeService?.DoWork();
    }

    /// <summary>
    /// Demonstrates circular dependency detection.
    /// </summary>
    public static void CircularDependencyExample()
    {
        using var serviceProvider = new ServiceProvider();

        serviceProvider.RegisterTransient<ICircularServiceA, CircularServiceA>();
        serviceProvider.RegisterTransient<ICircularServiceB, CircularServiceB>();

        try
        {
            // This will throw an InvalidOperationException due to circular dependency
            serviceProvider.GetService<ICircularServiceA>();
        }
        catch (InvalidOperationException ex)
        {
            Console.WriteLine($"Circular dependency detected: {ex.Message}");
        }
    }
}

// Example interfaces and implementations
public interface ILogger
{
    void Log(string message);
}

public interface IMessageService
{
    void SendMessage(string message);
}

public interface IDataService
{
    string GetData();
}

public interface IAttributeService
{
    void DoWork();
}

public interface ICircularServiceA { }
public interface ICircularServiceB { }

public class ConsoleLogger : ILogger
{
    public void Log(string message) => Console.WriteLine($"[LOG] {message}");
}

public class MessageService : IMessageService
{
    private readonly ILogger _logger;

    public MessageService(ILogger logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public void SendMessage(string message)
    {
        _logger.Log($"Sending message: {message}");
        // Send message logic here
    }
}

public class DataService : IDataService
{
    public string GetData() => "Sample data from service";
}

[Service("AttributeService", "1.0.0", "Example service registered via attribute")]
public class AttributeService : IAttributeService
{
    public void DoWork() => Console.WriteLine("AttributeService is working!");
}

public class CircularServiceA : ICircularServiceA
{
    public CircularServiceA(ICircularServiceB serviceB) { }
}

public class CircularServiceB : ICircularServiceB
{
    public CircularServiceB(ICircularServiceA serviceA) { }
}