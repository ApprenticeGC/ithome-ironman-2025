using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Default implementation of the AI orchestration service.
/// Provides agent discovery, registration, and basic orchestration capabilities.
/// </summary>
public class AIOrchestrationService : IService
{
    private readonly ILogger<AIOrchestrationService> _logger;
    private readonly IAgentRegistry _agentRegistry;
    private AIProfile? _currentProfile;
    private bool _isRunning;

    public AIOrchestrationService(ILogger<AIOrchestrationService> logger, IAgentRegistry? agentRegistry = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _agentRegistry = agentRegistry ?? new AgentRegistry();
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI Orchestration Service");
        
        // Register some default agents for demonstration
        await RegisterDefaultAgentsAsync(cancellationToken);
        
        _logger.LogInformation("AI Orchestration Service initialized");
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI Orchestration Service");
        _isRunning = true;
        _logger.LogInformation("AI Orchestration Service started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI Orchestration Service");
        _isRunning = false;
        _logger.LogInformation("AI Orchestration Service stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public Task InitializeAsync(AIProfile profile, CancellationToken ct = default)
    {
        if (profile == null) throw new ArgumentNullException(nameof(profile));
        
        _logger.LogInformation("Initializing AI service with profile: TaskKind={TaskKind}, Model={Model}", 
            profile.TaskKind, profile.Model);
        
        _currentProfile = profile;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task ShutdownAsync(CancellationToken ct = default)
    {
        _logger.LogInformation("Shutting down AI service");
        return StopAsync(ct);
    }

    /// <inheritdoc />
    public Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));
        if (input == null) throw new ArgumentNullException(nameof(input));

        _logger.LogDebug("Invoking agent {AgentId} with input length {InputLength}", agentId, input.Length);
        
        // For now, return a mock response. In a real implementation, this would route to the actual agent.
        return Task.FromResult($"Mock response from agent {agentId} for input: {input.Substring(0, Math.Min(50, input.Length))}...");
    }

    /// <inheritdoc />
    public Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));
        if (input == null) throw new ArgumentNullException(nameof(input));

        _logger.LogDebug("Streaming from agent {AgentId} with input length {InputLength}", agentId, input.Length);
        
        // For now, return a mock stream. In a real implementation, this would route to the actual agent.
        async IAsyncEnumerable<string> MockStream()
        {
            yield return $"Stream chunk 1 from {agentId}";
            await Task.Delay(100, ct);
            yield return $"Stream chunk 2 from {agentId}";
            await Task.Delay(100, ct);
            yield return $"Final chunk from {agentId}";
        }

        return Task.FromResult(MockStream());
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableAgents()
    {
        return _agentRegistry.GetAgentIds();
    }

    /// <inheritdoc />
    public async Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));

        var metadata = await _agentRegistry.GetAgentAsync(agentId, ct);
        if (metadata == null)
        {
            throw new InvalidOperationException($"Agent with ID '{agentId}' not found");
        }

        return metadata;
    }

    /// <inheritdoc />
    public Task<string> CreateConversationAsync(string agentId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId)) throw new ArgumentException("AgentId cannot be null or empty", nameof(agentId));

        var conversationId = Guid.NewGuid().ToString();
        _logger.LogDebug("Created conversation {ConversationId} for agent {AgentId}", conversationId, agentId);
        
        return Task.FromResult(conversationId);
    }

    /// <inheritdoc />
    public Task<bool> EndConversationAsync(string conversationId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId)) throw new ArgumentException("ConversationId cannot be null or empty", nameof(conversationId));

        _logger.LogDebug("Ending conversation {ConversationId}", conversationId);
        
        return Task.FromResult(true);
    }

    /// <inheritdoc />
    public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
    {
        var capabilities = new[] { typeof(IService) };
        return Task.FromResult<IEnumerable<Type>>(capabilities);
    }

    /// <inheritdoc />
    public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
    {
        return Task.FromResult(typeof(T).IsAssignableFrom(typeof(IService)));
    }

    /// <inheritdoc />
    public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
    {
        if (typeof(T).IsAssignableFrom(typeof(IService)))
        {
            return Task.FromResult(this as T);
        }
        return Task.FromResult<T?>(null);
    }

    /// <summary>
    /// Register default agents for testing and demonstration.
    /// </summary>
    private async Task RegisterDefaultAgentsAsync(CancellationToken cancellationToken)
    {
        var defaultAgents = new[]
        {
            new AgentMetadata
            {
                AgentId = "director-001",
                Name = "Content Director",
                Description = "AI agent for procedural content generation and game directing",
                AgentType = "DirectorAgent",
                Version = "1.0.0",
                Capabilities = new[] { "EncounterGeneration", "FlavorTextGeneration", "DungeonAdaptation" },
                SupportedTaskKinds = new[] { TaskKind.RuntimeDirector.ToString() },
                IsAvailable = true,
                Configuration = new Dictionary<string, object>
                {
                    { "MaxEncounters", 10 },
                    { "DifficultyRange", "1-5" }
                }
            },
            new AgentMetadata
            {
                AgentId = "dialogue-001",
                Name = "Dialogue Writer",
                Description = "AI agent for generating dialogue and narrative content",
                AgentType = "DialogueAgent", 
                Version = "1.0.0",
                Capabilities = new[] { "DialogueGeneration", "QuestGeneration", "NarrativeWriting" },
                SupportedTaskKinds = new[] { TaskKind.EditorAuthoring.ToString(), TaskKind.RuntimeCodex.ToString() },
                IsAvailable = true,
                Configuration = new Dictionary<string, object>
                {
                    { "MaxDialogueLength", 500 },
                    { "NarrativeStyle", "Fantasy" }
                }
            },
            new AgentMetadata
            {
                AgentId = "codex-001",
                Name = "Game Codex",
                Description = "AI agent for providing player help and lore information",
                AgentType = "CodexAgent",
                Version = "1.0.0",
                Capabilities = new[] { "LoreQueries", "PlayerHelp", "GameGuidance" },
                SupportedTaskKinds = new[] { TaskKind.RuntimeCodex.ToString() },
                IsAvailable = true,
                Configuration = new Dictionary<string, object>
                {
                    { "ResponseLength", "short" },
                    { "SafetyLevel", "strict" }
                }
            }
        };

        foreach (var agent in defaultAgents)
        {
            await _agentRegistry.RegisterAgentAsync(agent, cancellationToken);
            _logger.LogDebug("Registered default agent: {AgentId} ({Name})", agent.AgentId, agent.Name);
        }
    }
}