using Akka.Actor;
using Microsoft.Extensions.Logging;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Actors;

/// <summary>
/// Context Manager actor - manages conversation state and context for AI interactions.
/// </summary>
public class ContextManagerActor : ReceiveActor, IWithTimers
{
    private readonly ILogger<ContextManagerActor> _logger;
    private readonly Dictionary<string, ConversationContext> _conversations = new();
    private readonly object _conversationsLock = new();

    public ITimerScheduler Timers { get; set; } = null!;

    public ContextManagerActor(ILogger<ContextManagerActor> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        Receive<CreateConversation>(HandleCreateConversation);
        Receive<EndConversation>(HandleEndConversation);
        Receive<GetConversationContext>(HandleGetConversationContext);
        Receive<UpdateConversationContext>(HandleUpdateConversationContext);
        Receive<CleanupExpiredConversations>(HandleCleanupExpiredConversations);

        _logger.LogInformation("ContextManagerActor started");
    }

    protected override void PreStart()
    {
        _logger.LogInformation("ContextManagerActor starting");
        
        // Schedule periodic cleanup of expired conversations
        Timers.StartPeriodicTimer(
            "cleanup-expired-conversations",
            new CleanupExpiredConversations(),
            TimeSpan.FromMinutes(5),
            TimeSpan.FromMinutes(5));
        
        base.PreStart();
    }

    private void HandleCreateConversation(CreateConversation message)
    {
        var conversationId = Guid.NewGuid().ToString();
        var context = new ConversationContext(conversationId, message.AgentId, DateTimeOffset.UtcNow);
        
        lock (_conversationsLock)
        {
            _conversations[conversationId] = context;
        }

        _logger.LogDebug("Created conversation {ConversationId} for agent {AgentId}", conversationId, message.AgentId);
        Sender.Tell(new ConversationCreated(conversationId, message.AgentId));
    }

    private void HandleEndConversation(EndConversation message)
    {
        bool success = false;
        
        lock (_conversationsLock)
        {
            if (_conversations.TryGetValue(message.ConversationId, out var context))
            {
                context.EndedAt = DateTimeOffset.UtcNow;
                context.IsActive = false;
                success = true;
                _logger.LogDebug("Ended conversation {ConversationId}", message.ConversationId);
            }
            else
            {
                _logger.LogWarning("Attempted to end non-existent conversation {ConversationId}", message.ConversationId);
            }
        }

        Sender.Tell(new ConversationEnded(message.ConversationId, success));
    }

    private void HandleGetConversationContext(GetConversationContext message)
    {
        ConversationContext? context = null;
        
        lock (_conversationsLock)
        {
            _conversations.TryGetValue(message.ConversationId, out context);
        }

        if (context != null)
        {
            _logger.LogDebug("Retrieved context for conversation {ConversationId}", message.ConversationId);
            Sender.Tell(new ConversationContextResponse(message.ConversationId, context, true));
        }
        else
        {
            _logger.LogWarning("Context not found for conversation {ConversationId}", message.ConversationId);
            Sender.Tell(new ConversationContextResponse(message.ConversationId, null, false, "Conversation not found"));
        }
    }

    private void HandleUpdateConversationContext(UpdateConversationContext message)
    {
        bool success = false;
        
        lock (_conversationsLock)
        {
            if (_conversations.TryGetValue(message.ConversationId, out var context))
            {
                context.Messages.AddRange(message.NewMessages);
                context.LastUpdatedAt = DateTimeOffset.UtcNow;
                
                // Apply any metadata updates
                foreach (var kvp in message.MetadataUpdates)
                {
                    context.Metadata[kvp.Key] = kvp.Value;
                }
                
                success = true;
                _logger.LogDebug("Updated context for conversation {ConversationId} with {MessageCount} new messages", 
                    message.ConversationId, message.NewMessages.Count);
            }
            else
            {
                _logger.LogWarning("Attempted to update non-existent conversation {ConversationId}", message.ConversationId);
            }
        }

        Sender.Tell(new ConversationContextUpdated(message.ConversationId, success));
    }

    private void HandleCleanupExpiredConversations(CleanupExpiredConversations message)
    {
        var cutoffTime = DateTimeOffset.UtcNow.AddHours(-24); // Clean up conversations older than 24 hours
        var expiredConversations = new List<string>();
        
        lock (_conversationsLock)
        {
            foreach (var kvp in _conversations.ToList())
            {
                var context = kvp.Value;
                if (!context.IsActive && (context.EndedAt ?? context.LastUpdatedAt) < cutoffTime)
                {
                    expiredConversations.Add(kvp.Key);
                    _conversations.Remove(kvp.Key);
                }
            }
        }

        if (expiredConversations.Count > 0)
        {
            _logger.LogInformation("Cleaned up {Count} expired conversations", expiredConversations.Count);
        }
    }

    protected override void PostStop()
    {
        _logger.LogInformation("ContextManagerActor stopped");
        Timers.CancelAll();
        base.PostStop();
    }
}

/// <summary>
/// Context information for a conversation.
/// </summary>
public class ConversationContext
{
    public string ConversationId { get; }
    public string AgentId { get; }
    public DateTimeOffset CreatedAt { get; }
    public DateTimeOffset LastUpdatedAt { get; set; }
    public DateTimeOffset? EndedAt { get; set; }
    public bool IsActive { get; set; } = true;
    public List<ConversationMessage> Messages { get; } = new();
    public Dictionary<string, object> Metadata { get; } = new();

    public ConversationContext(string conversationId, string agentId, DateTimeOffset createdAt)
    {
        ConversationId = conversationId;
        AgentId = agentId;
        CreatedAt = createdAt;
        LastUpdatedAt = createdAt;
    }
}

/// <summary>
/// A message in a conversation.
/// </summary>
public record ConversationMessage(
    string Role, // "user", "assistant", "system"
    string Content,
    DateTimeOffset Timestamp,
    Dictionary<string, object>? Metadata = null);

// Context management messages
public record GetConversationContext(string ConversationId) : AIMessage;
public record ConversationContextResponse(string ConversationId, ConversationContext? Context, bool Success, string? ErrorMessage = null) : AIMessage;
public record UpdateConversationContext(string ConversationId, List<ConversationMessage> NewMessages, Dictionary<string, object> MetadataUpdates) : AIMessage;
public record ConversationContextUpdated(string ConversationId, bool Success) : AIMessage;
public record CleanupExpiredConversations : AIMessage;