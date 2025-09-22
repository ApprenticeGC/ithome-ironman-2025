using GameConsole.Core.Abstractions;
using GameConsole.AI.Core;

namespace GameConsole.AI.Core.Tests;

/// <summary>
/// Test implementation of IAiAgent for testing purposes.
/// </summary>
internal sealed class TestAiAgent : IAiAgent
{
    private readonly List<string> _capabilities;
    private AiAgentStatus _status = AiAgentStatus.Inactive;

    public TestAiAgent(string agentId, string name, string description, params string[] capabilities)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        _capabilities = new List<string>(capabilities);
    }

    public string AgentId { get; }
    public string Name { get; }
    public string Description { get; }
    public IReadOnlyList<string> Capabilities => _capabilities.AsReadOnly();
    public AiAgentStatus Status => _status;
    public bool IsRunning { get; private set; }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _status = AiAgentStatus.Initializing;
        return Task.CompletedTask;
    }

    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _status = AiAgentStatus.Ready;
        IsRunning = true;
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _status = AiAgentStatus.Inactive;
        IsRunning = false;
        return Task.CompletedTask;
    }

    public Task<AiAgentResponse> ProcessAsync(AiAgentRequest request, CancellationToken cancellationToken = default)
    {
        if (_status != AiAgentStatus.Ready)
            return Task.FromResult(AiAgentResponse.CreateError(request.Id, "Agent is not ready"));

        if (!_capabilities.Contains(request.Capability))
            return Task.FromResult(AiAgentResponse.CreateError(request.Id, $"Capability '{request.Capability}' not supported"));

        _status = AiAgentStatus.Processing;
        var result = $"Processed {request.Capability} request with data: {request.Data}";
        _status = AiAgentStatus.Ready;

        return Task.FromResult(AiAgentResponse.CreateSuccess(request.Id, result));
    }

    public ValueTask DisposeAsync()
    {
        _status = AiAgentStatus.Inactive;
        IsRunning = false;
        return ValueTask.CompletedTask;
    }
}