using GameConsole.AI.Discovery;
using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Discovery.Tests;

/// <summary>
/// Test implementation of IAIAgent for testing purposes.
/// </summary>
[AIAgentMetadata(
    Id = "test-agent-1",
    Name = "Test Agent",
    Description = "A test agent for unit testing",
    Version = "1.0.0",
    Priority = 5,
    Tags = new[] { "test", "basic" })]
[AIAgentResourceRequirements(
    MinMemoryBytes = 1024 * 1024, // 1MB
    RequiredCpuCores = 1,
    RequiresGpu = false,
    NetworkAccess = NetworkAccessLevel.None)]
public class TestAIAgent : IAIAgent, ITestCapability
{
    public string Id { get; } = "test-agent-1";
    public string Name { get; } = "Test Agent";
    public AgentStatus Status { get; private set; } = AgentStatus.Uninitialized;

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new[] { typeof(ITestCapability) });
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ITestCapability));
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ITestCapability))
        {
            return Task.FromResult<T?>(new TestCapability() as T);
        }
        return Task.FromResult<T?>(null);
    }

    public Task InitializeAsync(AgentInitializationContext context, CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.Initializing;
        // Simulate initialization work
        Status = AgentStatus.Ready;
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.ShuttingDown;
        Status = AgentStatus.Shutdown;
        return Task.CompletedTask;
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Status == AgentStatus.Ready);
    }

    public string GetTestResult() => "Test successful";
}

/// <summary>
/// Test capability interface for testing purposes.
/// </summary>
public interface ITestCapability
{
    string GetTestResult();
}

/// <summary>
/// Test capability implementation for testing purposes.
/// </summary>
public class TestCapability : ITestCapability
{
    public string GetTestResult() => "Test successful";
}

/// <summary>
/// Another test agent without metadata attributes.
/// </summary>
public class BasicTestAgent : IAIAgent
{
    public string Id { get; } = "basic-test-agent";
    public string Name { get; } = "Basic Test Agent";
    public AgentStatus Status { get; private set; } = AgentStatus.Uninitialized;

    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(Array.Empty<Type>());
    }

    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    public Task InitializeAsync(AgentInitializationContext context, CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.Ready;
        return Task.CompletedTask;
    }

    public Task ShutdownAsync(CancellationToken cancellationToken = default)
    {
        Status = AgentStatus.Shutdown;
        return Task.CompletedTask;
    }

    public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Status == AgentStatus.Ready);
    }
}