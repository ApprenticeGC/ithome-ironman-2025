using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services
{
    /// <summary>
    /// Simple example AI agent implementation for testing the clustering functionality.
    /// </summary>
    public class ExampleAIAgent : IAIAgent
    {
        private readonly ILogger<ExampleAIAgent> _logger;
        private volatile bool _isActive;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExampleAIAgent"/> class.
        /// </summary>
        /// <param name="agentId">Unique identifier for this agent.</param>
        /// <param name="agentType">Type/category of this agent.</param>
        /// <param name="logger">Logger instance for diagnostics.</param>
        public ExampleAIAgent(string agentId, string agentType, ILogger<ExampleAIAgent> logger)
        {
            AgentId = agentId ?? throw new ArgumentNullException(nameof(agentId));
            AgentType = agentType ?? throw new ArgumentNullException(nameof(agentType));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc />
        public string AgentId { get; }

        /// <inheritdoc />
        public string AgentType { get; }

        /// <inheritdoc />
        public bool IsActive => _isActive;

        /// <inheritdoc />
        public async Task<AIAgentResponse> ProcessMessageAsync(AIAgentMessage message, CancellationToken cancellationToken = default)
        {
            if (!_isActive)
            {
                return new AIAgentResponse(message.Id, AgentId, 
                    "Agent is not active", false, "Agent must be activated before processing messages");
            }

            _logger.LogDebug("Processing message {MessageId} of type {MessageType}", message.Id, message.MessageType);

            // Simulate some processing time
            await Task.Delay(100, cancellationToken);

            var responseContent = $"Echo: {message.Content} (processed by {AgentId} of type {AgentType})";
            
            return new AIAgentResponse(message.Id, AgentId, responseContent, true);
        }

        /// <inheritdoc />
        public Task ActivateAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Activating AI agent {AgentId}", AgentId);
            _isActive = true;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task DeactivateAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Deactivating AI agent {AgentId}", AgentId);
            _isActive = false;
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task<IEnumerable<Type>> GetCapabilitiesAsync(CancellationToken cancellationToken = default)
        {
            // Return the capabilities this agent provides
            return Task.FromResult<IEnumerable<Type>>(new[] { typeof(IAIAgent) });
        }

        /// <inheritdoc />
        public Task<bool> HasCapabilityAsync<T>(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(typeof(T) == typeof(IAIAgent));
        }

        /// <inheritdoc />
        public Task<T?> GetCapabilityAsync<T>(CancellationToken cancellationToken = default) where T : class
        {
            if (typeof(T) == typeof(IAIAgent))
            {
                return Task.FromResult(this as T);
            }
            return Task.FromResult<T?>(null);
        }
    }
}