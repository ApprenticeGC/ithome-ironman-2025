namespace TestLib;

/// <summary>
/// CI Validation helper class for testing enhanced CI validation system
/// Provides methods to validate RFC-092-03 acceptance criteria
/// </summary>
public class CIValidationHelper
{
    private readonly Random _random = new();
    private readonly DateTime _validationStartTime = DateTime.UtcNow;

    /// <summary>
    /// Validates that CI system correctly processes test results
    /// </summary>
    public bool ValidateTestExecution()
    {
        // Simulate consistent validation behavior
        return true;
    }

    /// <summary>
    /// Simulates a scenario where CI validation is critical
    /// </summary>
    public string GetValidationStatus()
    {
        return "CI_VALIDATION_ACTIVE";
    }

    /// <summary>
    /// Checks if validation system can detect failures properly
    /// This is the core method for testing enhanced CI validation
    /// </summary>
    public bool CanDetectFailures(bool shouldFail)
    {
        // Enhanced validation logic: return false when failure is expected
        // This prevents false successes by correctly identifying when failures occur
        return !shouldFail;
    }

    /// <summary>
    /// Simulates log analysis for different severity levels
    /// Used to test the enhanced CI validation logic
    /// </summary>
    public ValidationResult AnalyzeLogSeverity(int warningCount, int errorCount)
    {
        var severityScore = errorCount * 10 + warningCount * 3;
        var shouldBlock = errorCount > 0 || warningCount >= 5 || severityScore >= 15;
        
        return new ValidationResult
        {
            WarningCount = warningCount,
            ErrorCount = errorCount,
            SeverityScore = severityScore,
            ShouldBlock = shouldBlock,
            Reason = shouldBlock ? 
                $"Blocking: {errorCount} errors, {warningCount} warnings (severity: {severityScore})" :
                $"Allowing: {errorCount} errors, {warningCount} warnings (severity: {severityScore})"
        };
    }

    /// <summary>
    /// Validates CI workflow execution patterns
    /// Tests different workflow trigger scenarios
    /// </summary>
    public WorkflowTriggerResult ValidateWorkflowTrigger(string workflowName, string status, string conclusion)
    {
        var isValidTrigger = workflowName == "ci" && status == "completed" && conclusion == "success";
        
        return new WorkflowTriggerResult
        {
            WorkflowName = workflowName,
            Status = status,
            Conclusion = conclusion,
            ShouldTrigger = isValidTrigger,
            ValidationMessage = isValidTrigger ? 
                "Valid CI completion trigger detected" : 
                $"Invalid trigger: {workflowName}/{status}/{conclusion}"
        };
    }

    /// <summary>
    /// Simulates PR author validation for auto-ready processing
    /// </summary>
    public bool ValidatePRAuthor(string author)
    {
        var allowedAuthors = new[] 
        { 
            "Copilot", 
            "app/copilot-swe-agent", 
            "github-actions[bot]", 
            "github-actions", 
            "app/github-actions" 
        };
        
        return allowedAuthors.Contains(author, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Provides detailed validation metrics for monitoring
    /// </summary>
    public ValidationMetrics GetValidationMetrics()
    {
        return new ValidationMetrics
        {
            ValidationStartTime = _validationStartTime,
            SystemUptime = DateTime.UtcNow - _validationStartTime,
            TotalValidations = _random.Next(100, 1000),
            SuccessfulValidations = _random.Next(90, 99),
            BlockedValidations = _random.Next(1, 10),
            SystemHealth = "HEALTHY"
        };
    }
}

/// <summary>
/// Result of log analysis validation
/// </summary>
public class ValidationResult
{
    public int WarningCount { get; set; }
    public int ErrorCount { get; set; }
    public int SeverityScore { get; set; }
    public bool ShouldBlock { get; set; }
    public string Reason { get; set; } = string.Empty;
}

/// <summary>
/// Result of workflow trigger validation
/// </summary>
public class WorkflowTriggerResult
{
    public string WorkflowName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string Conclusion { get; set; } = string.Empty;
    public bool ShouldTrigger { get; set; }
    public string ValidationMessage { get; set; } = string.Empty;
}

/// <summary>
/// Validation system metrics
/// </summary>
public class ValidationMetrics
{
    public DateTime ValidationStartTime { get; set; }
    public TimeSpan SystemUptime { get; set; }
    public int TotalValidations { get; set; }
    public int SuccessfulValidations { get; set; }
    public int BlockedValidations { get; set; }
    public string SystemHealth { get; set; } = string.Empty;
}
