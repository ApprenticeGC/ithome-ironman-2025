namespace GameConsole.AI.Core.Models;

/// <summary>
/// Represents the result of an AI execution operation,
/// including the output data and execution metadata.
/// </summary>
public class AIExecutionResult
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AIExecutionResult"/> class.
    /// </summary>
    /// <param name="output">The output data from the AI operation.</param>
    /// <param name="isSuccess">Whether the operation completed successfully.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    public AIExecutionResult(string output, bool isSuccess, long executionTimeMs)
    {
        Output = output ?? throw new ArgumentNullException(nameof(output));
        IsSuccess = isSuccess;
        ExecutionTimeMs = executionTimeMs;
        Timestamp = DateTimeOffset.UtcNow;
        Metadata = new Dictionary<string, object>();
    }

    /// <summary>
    /// Gets the output data from the AI operation.
    /// </summary>
    public string Output { get; }

    /// <summary>
    /// Gets a value indicating whether the operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; }

    /// <summary>
    /// Gets the timestamp when the result was created.
    /// </summary>
    public DateTimeOffset Timestamp { get; }

    /// <summary>
    /// Gets or sets the error message if the operation failed.
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Gets or sets the error code if the operation failed.
    /// </summary>
    public string? ErrorCode { get; init; }

    /// <summary>
    /// Gets additional metadata about the execution.
    /// </summary>
    public IReadOnlyDictionary<string, object> Metadata { get; private set; }

    /// <summary>
    /// Gets the confidence score of the result, if applicable.
    /// </summary>
    public double? ConfidenceScore { get; init; }

    /// <summary>
    /// Sets additional metadata for the execution result.
    /// </summary>
    /// <param name="metadata">The metadata to set.</param>
    /// <returns>This instance for method chaining.</returns>
    public AIExecutionResult WithMetadata(IReadOnlyDictionary<string, object> metadata)
    {
        Metadata = metadata ?? throw new ArgumentNullException(nameof(metadata));
        return this;
    }

    /// <summary>
    /// Creates a successful execution result.
    /// </summary>
    /// <param name="output">The output data.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <returns>A successful execution result.</returns>
    public static AIExecutionResult Success(string output, long executionTimeMs) =>
        new(output, true, executionTimeMs);

    /// <summary>
    /// Creates a failed execution result.
    /// </summary>
    /// <param name="errorMessage">The error message.</param>
    /// <param name="executionTimeMs">The execution time in milliseconds.</param>
    /// <param name="errorCode">Optional error code.</param>
    /// <returns>A failed execution result.</returns>
    public static AIExecutionResult Failure(string errorMessage, long executionTimeMs, string? errorCode = null) =>
        new(string.Empty, false, executionTimeMs)
        {
            ErrorMessage = errorMessage,
            ErrorCode = errorCode
        };
}