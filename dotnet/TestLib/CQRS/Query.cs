namespace TestLib.CQRS;

/// <summary>
/// Abstract base class for queries in the CQRS system.
/// Provides common functionality and tracking for query execution.
/// </summary>
/// <typeparam name="TResult">The type of result returned by the query.</typeparam>
public abstract class Query<TResult> : IQuery<TResult>
{
    /// <summary>
    /// Unique identifier for this query instance.
    /// </summary>
    public Guid Id { get; } = Guid.NewGuid();

    /// <summary>
    /// Timestamp when the query was created.
    /// </summary>
    public DateTime Timestamp { get; } = DateTime.UtcNow;

    /// <summary>
    /// Optional user or system identifier that initiated the query.
    /// </summary>
    public string? InitiatedBy { get; set; }
}