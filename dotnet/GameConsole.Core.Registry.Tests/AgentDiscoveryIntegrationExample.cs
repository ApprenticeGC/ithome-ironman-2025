using System;
using System.Reflection;
using System.Threading.Tasks;
using GameConsole.Core.Registry.Examples;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Demonstrates the complete AI Agent Discovery and Registration system.
/// This example shows the end-to-end functionality working as intended.
/// </summary>
public class AgentDiscoveryIntegrationExample
{
    public static async Task RunCompleteExampleAsync()
    {
        Console.WriteLine("=== Complete AI Agent Discovery and Registration Demo ===");
        
        await AgentDiscoveryExample.RunExampleAsync();
        
        Console.WriteLine("\n=== Integration Example Completed Successfully! ===");
    }
}