using GameConsole.Core.Abstractions;

namespace GameConsole.Core.Registry.Tests;

/// <summary>
/// Test capability interface for testing capability-based discovery.
/// </summary>
public interface ITestCapability
{
    string GetTestData();
}

/// <summary>
/// Test AI agent implementation for unit tests.
/// </summary>
public class TestAIAgent : IAIAgent
{
    private readonly List<string> _categories;
    private AIAgentStatus _status;
    private bool _isRunning;
    private bool _disposed;

    public TestAIAgent(string id, string name, string? description = null, string? version = null, string[]? categories = null)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? $"Test AI agent: {name}";
        Version = version ?? "1.0.0";
        _categories = categories?.ToList() ?? new List<string>();
        _status = AIAgentStatus.NotInitialized;
    }

    public string Id { get; }
    public string Name { get; }
    public string Version { get; }
    public string Description { get; }
    public IReadOnlyList<string> Categories => _categories;
    public AIAgentStatus Status => _status;
    public bool IsRunning => _isRunning;

    public virtual Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new List<Type>());
    }

    public virtual Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(false);
    }

    public virtual Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        return Task.FromResult<T?>(null);
    }

    public virtual Task<IAIAgentResponse> ExecuteAsync(IAIAgentRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        
        var response = new AIAgentResponse(
            request.RequestId,
            $"Processed by {Name}",
            new Dictionary<string, object> { ["processingAgent"] = Name },
            TimeSpan.FromMilliseconds(100));
        
        return Task.FromResult<IAIAgentResponse>(response);
    }

    public virtual Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _status = AIAgentStatus.Initializing;
        _status = AIAgentStatus.Ready;
        return Task.CompletedTask;
    }

    public virtual Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_status != AIAgentStatus.Ready)
            throw new InvalidOperationException("Agent must be initialized before starting");
        
        _isRunning = true;
        return Task.CompletedTask;
    }

    public virtual Task StopAsync(CancellationToken cancellationToken = default)
    {
        _isRunning = false;
        return Task.CompletedTask;
    }

    public virtual ValueTask DisposeAsync()
    {
        if (_disposed) return ValueTask.CompletedTask;

        _status = AIAgentStatus.Disposing;
        _isRunning = false;
        _disposed = true;
        _status = AIAgentStatus.Disposed;
        
        return ValueTask.CompletedTask;
    }
}

/// <summary>
/// Test AI agent that provides a specific capability for capability-based discovery tests.
/// </summary>
public class TestAIAgentWithCapability : TestAIAgent, ITestCapability
{
    public TestAIAgentWithCapability(string id, string name, string? description = null, string? version = null, string[]? categories = null)
        : base(id, name, description, version, categories)
    {
    }

    public override Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        return Task.FromResult<IEnumerable<Type>>(new List<Type> { typeof(ITestCapability) });
    }

    public override Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T) == typeof(ITestCapability));
    }

    public override Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T) == typeof(ITestCapability))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    public string GetTestData()
    {
        return $"Test data from {Name}";
    }
}