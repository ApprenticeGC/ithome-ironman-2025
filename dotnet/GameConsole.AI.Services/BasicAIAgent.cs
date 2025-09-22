using GameConsole.Core.Abstractions;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services;

/// <summary>
/// Basic implementation of an AI agent that can process tasks and communicate with other agents.
/// This is a Tier 3 service that provides the core behavior for AI agents.
/// </summary>
public class BasicAIAgent : IAIAgent
{
    private readonly ILogger<BasicAIAgent> _logger;
    private readonly ConcurrentQueue<(string targetId, object message)> _messageQueue = new();
    private readonly CancellationTokenSource _cancellationTokenSource = new();
    private AgentState _state = AgentState.Idle;
    private Task? _executionTask;

    /// <inheritdoc />
    public event EventHandler<AgentEventArgs>? StateChanged;

    /// <inheritdoc />
    public event EventHandler<AgentEventArgs>? MessageReceived;

    /// <inheritdoc />
    public string AgentId { get; }

    /// <inheritdoc />
    public AgentState State 
    { 
        get => _state;
        private set
        {
            if (_state != value)
            {
                var oldState = _state;
                _state = value;
                _logger.LogDebug("Agent {AgentId} state changed from {OldState} to {NewState}", AgentId, oldState, value);
                StateChanged?.Invoke(this, new AgentEventArgs(AgentId, value));
            }
        }
    }

    /// <inheritdoc />
    public string AgentType { get; }

    /// <summary>
    /// Initializes a new instance of the BasicAIAgent class.
    /// </summary>
    /// <param name="agentId">The unique identifier for this agent.</param>
    /// <param name="agentType">The type of this agent.</param>
    /// <param name="logger">Logger for this agent.</param>
    public BasicAIAgent(string agentId, string agentType, ILogger<BasicAIAgent> logger)
    {
        AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
        AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing agent {AgentId} of type {AgentType}", AgentId, AgentType);
        State = AgentState.Idle;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting agent {AgentId}", AgentId);
        
        if (_executionTask != null)
        {
            _logger.LogWarning("Agent {AgentId} is already running", AgentId);
            return;
        }

        _executionTask = ExecutionLoopAsync(_cancellationTokenSource.Token);
        State = AgentState.Processing;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task PauseAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Pausing agent {AgentId}", AgentId);
        State = AgentState.Paused;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ResumeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Resuming agent {AgentId}", AgentId);
        State = AgentState.Processing;
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping agent {AgentId}", AgentId);
        
        _cancellationTokenSource.Cancel();
        
        if (_executionTask != null)
        {
            await _executionTask;
            _executionTask = null;
        }
        
        State = AgentState.Idle;
    }

    /// <inheritdoc />
    public async Task SendMessageAsync(string targetAgentId, object message, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Agent {AgentId} sending message to {TargetAgentId}", AgentId, targetAgentId);
        _messageQueue.Enqueue((targetAgentId, message));
        await Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task<object?> ProcessTaskAsync(object task, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Agent {AgentId} processing task of type {TaskType}", AgentId, task.GetType().Name);
        
        // Simulate task processing
        await Task.Delay(100, cancellationToken);
        
        // Basic task processing - in real implementation this would be more sophisticated
        return $"Processed by {AgentId}: {task}";
    }

    /// <summary>
    /// Internal method to receive a message from another agent.
    /// </summary>
    /// <param name="fromAgentId">The sender agent ID.</param>
    /// <param name="message">The message received.</param>
    internal void ReceiveMessage(string fromAgentId, object message)
    {
        _logger.LogDebug("Agent {AgentId} received message from {FromAgentId}", AgentId, fromAgentId);
        MessageReceived?.Invoke(this, new AgentEventArgs(AgentId, State, message));
    }

    private async Task ExecutionLoopAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Agent {AgentId} execution loop started", AgentId);
        
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                // Process pending messages
                while (_messageQueue.TryDequeue(out var messageItem))
                {
                    // In a real implementation, this would route messages to the target agent
                    _logger.LogDebug("Agent {AgentId} processed queued message to {TargetId}", AgentId, messageItem.targetId);
                }
                
                // Small delay to prevent CPU spinning
                await Task.Delay(50, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in agent {AgentId} execution loop", AgentId);
                State = AgentState.Error;
                break;
            }
        }
        
        _logger.LogDebug("Agent {AgentId} execution loop stopped", AgentId);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (State != AgentState.Disposed)
        {
            await StopAsync();
            _cancellationTokenSource.Dispose();
            State = AgentState.Disposed;
            _logger.LogInformation("Agent {AgentId} disposed", AgentId);
        }
    }
}