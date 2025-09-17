namespace TestLib.CQRS;

/// <summary>
/// Abstract base class for commands in the CQRS system.
/// Provides common functionality and tracking for command execution.
/// </summary>
public abstract class Command : ICommand
{
    /// <summary>
    /// Unique identifier for this command instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the command was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Optional user or system identifier that initiated the command.
    /// </summary>
    public string? InitiatedBy { get; set; }
}