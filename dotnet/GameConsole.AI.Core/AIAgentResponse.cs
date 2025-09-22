using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Represents a response from an AI agent after processing a message.
    /// </summary>
    public class AIAgentResponse
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AIAgentResponse"/> class.
        /// </summary>
        /// <param name="messageId">The ID of the message this response is for.</param>
        /// <param name="agentId">The ID of the agent that generated this response.</param>
        /// <param name="content">The response content.</param>
        /// <param name="success">Indicates whether the message processing was successful.</param>
        /// <param name="errorMessage">Optional error message if processing failed.</param>
        /// <param name="metadata">Optional metadata associated with this response.</param>
        public AIAgentResponse(string messageId, string agentId, string content, bool success = true, 
            string? errorMessage = null, Dictionary<string, object>? metadata = null)
        {
            MessageId = messageId;
            AgentId = agentId;
            Content = content;
            Success = success;
            ErrorMessage = errorMessage;
            Metadata = metadata;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the ID of the message this response is for.
        /// </summary>
        public string MessageId { get; }

        /// <summary>
        /// Gets the ID of the agent that generated this response.
        /// </summary>
        public string AgentId { get; }

        /// <summary>
        /// Gets the response content.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets a value indicating whether the message processing was successful.
        /// </summary>
        public bool Success { get; }

        /// <summary>
        /// Gets the timestamp when the response was generated.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets optional error message if processing failed.
        /// </summary>
        public string? ErrorMessage { get; }

        /// <summary>
        /// Gets optional metadata associated with this response.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }
    }
}