using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core
{
    /// <summary>
    /// Represents a message sent to an AI agent for processing.
    /// </summary>
    public class AIAgentMessage
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="AIAgentMessage"/> class.
        /// </summary>
        /// <param name="id">Unique identifier for this message.</param>
        /// <param name="content">The message content.</param>
        /// <param name="messageType">The type of message.</param>
        /// <param name="senderId">Optional identifier of the message sender.</param>
        public AIAgentMessage(string id, string content, string messageType, string? senderId = null)
        {
            Id = id;
            Content = content;
            MessageType = messageType;
            SenderId = senderId;
            Timestamp = DateTimeOffset.UtcNow;
        }

        /// <summary>
        /// Gets the unique identifier for this message.
        /// </summary>
        public string Id { get; }

        /// <summary>
        /// Gets the message content.
        /// </summary>
        public string Content { get; }

        /// <summary>
        /// Gets the type of message (e.g., "Query", "Command", "Event").
        /// </summary>
        public string MessageType { get; }

        /// <summary>
        /// Gets the identifier of the message sender, if available.
        /// </summary>
        public string? SenderId { get; }

        /// <summary>
        /// Gets the timestamp when the message was created.
        /// </summary>
        public DateTimeOffset Timestamp { get; }

        /// <summary>
        /// Gets optional metadata associated with this message.
        /// </summary>
        public Dictionary<string, object>? Metadata { get; }
    }
}