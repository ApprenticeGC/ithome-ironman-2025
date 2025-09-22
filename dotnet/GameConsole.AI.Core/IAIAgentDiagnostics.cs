namespace GameConsole.AI.Core;

/// <summary>
/// Represents a diagnostic request for testing an AI agent's capabilities.
/// </summary>
public interface IAIAgentDiagnosticRequest
{
    /// <summary>
    /// Gets the type of diagnostic test to run.
    /// </summary>
    string DiagnosticType { get; }

    /// <summary>
    /// Gets the test data or input for the diagnostic.
    /// </summary>
    object TestData { get; }

    /// <summary>
    /// Gets the expected outcome or result for validation.
    /// </summary>
    object? ExpectedResult { get; }

    /// <summary>
    /// Gets additional parameters for the diagnostic test.
    /// </summary>
    IReadOnlyDictionary<string, object> Parameters { get; }
}

/// <summary>
/// Represents the result of a diagnostic test run on an AI agent.
/// </summary>
public interface IAIAgentDiagnosticResult
{
    /// <summary>
    /// Gets the type of diagnostic test that was run.
    /// </summary>
    string DiagnosticType { get; }

    /// <summary>
    /// Gets a value indicating whether the diagnostic test passed.
    /// </summary>
    bool Passed { get; }

    /// <summary>
    /// Gets the actual result from the diagnostic test.
    /// </summary>
    object? ActualResult { get; }

    /// <summary>
    /// Gets any error messages or details from the diagnostic test.
    /// </summary>
    string? ErrorDetails { get; }

    /// <summary>
    /// Gets performance metrics from the diagnostic test.
    /// </summary>
    IReadOnlyDictionary<string, object> Metrics { get; }

    /// <summary>
    /// Gets the timestamp when the diagnostic was completed.
    /// </summary>
    DateTimeOffset CompletedAt { get; }
}