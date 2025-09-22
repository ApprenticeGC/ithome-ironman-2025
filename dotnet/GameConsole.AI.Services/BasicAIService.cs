using GameConsole.AI.Services;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;

namespace GameConsole.AI.Services.Implementation;

/// <summary>
/// Basic implementation of the AI service providing agent discovery and registration.
/// This is a minimal Tier 2-3 implementation for RFC-007-02.
/// </summary>
public class BasicAIService : IService, IStreamingCapability, IConversationCapability
{
    private readonly ILogger<BasicAIService> _logger;
    private readonly ConcurrentDictionary<string, AgentMetadata> _registeredAgents = new();
    private readonly ConcurrentDictionary<string, ConversationContext> _conversations = new();
    private volatile bool _isRunning;

    /// <summary>
    /// Initializes a new instance of the <see cref="BasicAIService"/> class.
    /// </summary>
    /// <param name="logger">Logger for service operations.</param>
    public BasicAIService(ILogger<BasicAIService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public bool IsRunning => _isRunning;

    /// <inheritdoc />
    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Initializing AI service");
        
        // Register some default agents for demonstration
        RegisterBuiltInAgents();
        
        _logger.LogInformation("AI service initialized with {AgentCount} default agents", _registeredAgents.Count);
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StartAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting AI service");
        _isRunning = true;
        _logger.LogInformation("AI service started");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public Task StopAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Stopping AI service");
        _isRunning = false;
        
        // Clear all conversations
        _conversations.Clear();
        
        _logger.LogInformation("AI service stopped");
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public ValueTask DisposeAsync()
    {
        return new ValueTask(StopAsync());
    }

    /// <inheritdoc />
    public IEnumerable<string> GetAvailableAgents()
    {
        _logger.LogDebug("Getting available agents");
        return _registeredAgents.Keys.ToArray();
    }

    /// <inheritdoc />
    public Task<AgentMetadata?> GetAgentInfoAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        _logger.LogDebug("Getting agent info for {AgentId}", agentId);
        
        return Task.FromResult(_registeredAgents.TryGetValue(agentId, out var metadata) ? metadata : null);
    }

    /// <inheritdoc />
    public Task<bool> RegisterAgentAsync(AgentMetadata agentMetadata, CancellationToken cancellationToken = default)
    {
        if (agentMetadata == null)
        {
            throw new ArgumentNullException(nameof(agentMetadata));
        }

        if (string.IsNullOrWhiteSpace(agentMetadata.Id))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentMetadata));
        }

        _logger.LogInformation("Registering agent {AgentId} ({AgentName})", agentMetadata.Id, agentMetadata.Name);
        
        var success = _registeredAgents.TryAdd(agentMetadata.Id, agentMetadata);
        
        if (success)
        {
            _logger.LogInformation("Successfully registered agent {AgentId}", agentMetadata.Id);
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} already registered", agentMetadata.Id);
        }

        return Task.FromResult(success);
    }

    /// <inheritdoc />
    public Task<bool> UnregisterAgentAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        _logger.LogInformation("Unregistering agent {AgentId}", agentId);
        
        var success = _registeredAgents.TryRemove(agentId, out _);
        
        if (success)
        {
            _logger.LogInformation("Successfully unregistered agent {AgentId}", agentId);
        }
        else
        {
            _logger.LogWarning("Agent {AgentId} not found for unregistration", agentId);
        }

        return Task.FromResult(success);
    }

    /// <inheritdoc />
    public Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        if (string.IsNullOrWhiteSpace(input))
        {
            throw new ArgumentException("Input cannot be null or empty", nameof(input));
        }

        _logger.LogDebug("Invoking agent {AgentId} with input length {InputLength}", agentId, input.Length);

        if (!_registeredAgents.TryGetValue(agentId, out var agent))
        {
            throw new InvalidOperationException($"Agent '{agentId}' not found");
        }

        // For this minimal implementation, we'll return a simple echo response
        var response = $"[{agent.Name}]: Echo response to '{input}' (capabilities: {agent.Capabilities})";
        
        _logger.LogDebug("Agent {AgentId} responded with length {ResponseLength}", agentId, response.Length);
        
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<string> StreamAgentAsync(string agentId, string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        if (!_registeredAgents.TryGetValue(agentId, out var agent))
        {
            throw new InvalidOperationException($"Agent '{agentId}' not found");
        }

        _logger.LogDebug("Streaming from agent {AgentId}", agentId);

        // For demonstration, simulate streaming by yielding words with delays
        var words = $"Streaming response from {agent.Name} to input: {input}".Split(' ');
        
        foreach (var word in words)
        {
            cancellationToken.ThrowIfCancellationRequested();
            
            yield return word + " ";
            
            // Small delay to simulate real streaming
            await Task.Delay(50, cancellationToken);
        }
    }

    /// <inheritdoc />
    public Task<string> CreateConversationAsync(string agentId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            throw new ArgumentException("Agent ID cannot be null or empty", nameof(agentId));
        }

        if (!_registeredAgents.ContainsKey(agentId))
        {
            throw new InvalidOperationException($"Agent '{agentId}' not found");
        }

        var conversationId = Guid.NewGuid().ToString();
        var context = new ConversationContext(conversationId, agentId);
        
        _conversations.TryAdd(conversationId, context);
        
        _logger.LogDebug("Created conversation {ConversationId} for agent {AgentId}", conversationId, agentId);
        
        return Task.FromResult(conversationId);
    }

    /// <inheritdoc />
    public Task<bool> EndConversationAsync(string conversationId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
        }

        var success = _conversations.TryRemove(conversationId, out _);
        
        if (success)
        {
            _logger.LogDebug("Ended conversation {ConversationId}", conversationId);
        }
        else
        {
            _logger.LogWarning("Conversation {ConversationId} not found", conversationId);
        }
        
        return Task.FromResult(success);
    }

    /// <inheritdoc />
    public Task<string> InvokeInConversationAsync(string conversationId, string input, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(conversationId))
        {
            throw new ArgumentException("Conversation ID cannot be null or empty", nameof(conversationId));
        }

        if (!_conversations.TryGetValue(conversationId, out var conversation))
        {
            throw new InvalidOperationException($"Conversation '{conversationId}' not found");
        }

        conversation.AddMessage(input);
        
        // For this minimal implementation, include conversation history in response
        var messageCount = conversation.Messages.Count;
        var response = $"[Conversation {conversationId}] Message #{messageCount}: Echo to '{input}'";
        
        conversation.AddMessage(response);
        
        _logger.LogDebug("Invoked agent in conversation {ConversationId} (message #{MessageCount})", 
            conversationId, messageCount);
        
        return Task.FromResult(response);
    }

    /// <inheritdoc />
    public bool HasCapability<T>() where T : class
    {
        return typeof(T) == typeof(IStreamingCapability) || 
               typeof(T) == typeof(IConversationCapability);
    }

    /// <inheritdoc />
    public T? GetCapability<T>() where T : class
    {
        if (typeof(T) == typeof(IStreamingCapability))
        {
            return this as T;
        }
        
        if (typeof(T) == typeof(IConversationCapability))
        {
            return this as T;
        }

        return null;
    }

    private void RegisterBuiltInAgents()
    {
        // Register a few default agents for demonstration
        var textAgent = new AgentMetadata(
            "text-generator",
            "Text Generator",
            "Basic text generation agent for general purposes",
            AgentCapabilities.TextGeneration | AgentCapabilities.CreativeWriting)
        {
            Properties = new Dictionary<string, object>
            {
                ["model"] = "mock-gpt",
                ["temperature"] = 0.7,
                ["maxTokens"] = 1024
            }
        };

        var codeAgent = new AgentMetadata(
            "code-assistant",
            "Code Assistant", 
            "AI agent specialized in code analysis and generation",
            AgentCapabilities.CodeGeneration | AgentCapabilities.TextGeneration)
        {
            Properties = new Dictionary<string, object>
            {
                ["model"] = "mock-codex",
                ["temperature"] = 0.2,
                ["maxTokens"] = 2048
            }
        };

        var dialogueAgent = new AgentMetadata(
            "dialogue-master",
            "Dialogue Master",
            "Conversational AI agent for character dialogue and NPC interactions",
            AgentCapabilities.Dialogue | AgentCapabilities.CreativeWriting | AgentCapabilities.GameContentGeneration)
        {
            Properties = new Dictionary<string, object>
            {
                ["model"] = "mock-chat",
                ["personality"] = "friendly",
                ["contextWindow"] = 4096
            }
        };

        _registeredAgents.TryAdd(textAgent.Id, textAgent);
        _registeredAgents.TryAdd(codeAgent.Id, codeAgent);
        _registeredAgents.TryAdd(dialogueAgent.Id, dialogueAgent);
    }
}

/// <summary>
/// Represents a conversation context for maintaining state across multiple interactions.
/// </summary>
internal class ConversationContext
{
    public string Id { get; }
    public string AgentId { get; }
    public List<string> Messages { get; } = new();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    public ConversationContext(string id, string agentId)
    {
        Id = id;
        AgentId = agentId;
    }

    public void AddMessage(string message)
    {
        Messages.Add(message);
    }
}