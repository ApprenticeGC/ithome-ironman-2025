using Microsoft.Extensions.Logging;

namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Manages deployment stages with approval gates and validation.
/// </summary>
public class DeploymentStageManager : IDeploymentStageManager
{
    private readonly ILogger<DeploymentStageManager> _logger;
    private readonly Dictionary<string, Dictionary<DeploymentStage, ApprovalStatus>> _approvals = new();

    public DeploymentStageManager(ILogger<DeploymentStageManager> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<StageExecutionResult> ExecuteStageAsync(
        DeploymentContext context,
        DeploymentStage stage,
        StageConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing deployment stage {Stage} for deployment {DeploymentId}",
            stage, context.DeploymentId);

        var startTime = DateTimeOffset.UtcNow;
        var result = new StageExecutionResult
        {
            Stage = stage,
            Success = false
        };

        try
        {
            // Validate stage prerequisites
            var validationResult = await ValidateStageAsync(context, stage, configuration, cancellationToken);
            if (!validationResult.CanProceed)
            {
                result.ErrorMessage = validationResult.BlockingReason;
                return result;
            }

            // Execute stage-specific logic
            await ExecuteStageLogicAsync(context, stage, configuration, result, cancellationToken);

            // Run validation tests if configured
            if (configuration.ValidationTests.Any())
            {
                await RunValidationTestsAsync(configuration.ValidationTests, result, cancellationToken);
            }

            result.Success = true;
            _logger.LogInformation("Successfully completed deployment stage {Stage} for deployment {DeploymentId}",
                stage, context.DeploymentId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to execute deployment stage {Stage} for deployment {DeploymentId}",
                stage, context.DeploymentId);
            result.Exception = ex;
            result.ErrorMessage = ex.Message;
        }
        finally
        {
            result.Duration = DateTimeOffset.UtcNow - startTime;
        }

        return result;
    }

    /// <inheritdoc />
    public async Task<StageValidationResult> ValidateStageAsync(
        DeploymentContext context,
        DeploymentStage stage,
        StageConfiguration configuration,
        CancellationToken cancellationToken = default)
    {
        var result = new StageValidationResult { CanProceed = true };

        try
        {
            // Check if approval is required
            if (configuration.RequiresApproval)
            {
                var approvalStatus = await GetApprovalStatusAsync(context.DeploymentId, stage, cancellationToken);
                if (approvalStatus == null || !approvalStatus.IsApproved)
                {
                    result.CanProceed = false;
                    result.WaitingForApproval = true;
                    result.RequiredApprovers = configuration.Approvers.ToList();
                    result.BlockingReason = $"Stage {stage} requires approval from: {string.Join(", ", configuration.Approvers)}";
                }
            }

            // Additional validation logic could be added here
            // e.g., check environment health, resource availability, etc.

            _logger.LogDebug("Stage validation result for {Stage}: CanProceed={CanProceed}, WaitingForApproval={WaitingForApproval}",
                stage, result.CanProceed, result.WaitingForApproval);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to validate deployment stage {Stage} for deployment {DeploymentId}",
                stage, context.DeploymentId);
            result.CanProceed = false;
            result.BlockingReason = $"Validation failed: {ex.Message}";
        }

        return result;
    }

    /// <inheritdoc />
    public Task<bool> RecordApprovalAsync(
        string deploymentId,
        DeploymentStage stage,
        string approvedBy,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (!_approvals.ContainsKey(deploymentId))
            {
                _approvals[deploymentId] = new Dictionary<DeploymentStage, ApprovalStatus>();
            }

            _approvals[deploymentId][stage] = new ApprovalStatus
            {
                IsApproved = true,
                ApprovedBy = approvedBy,
                ApprovedAt = DateTimeOffset.UtcNow,
                Comments = $"Approved by {approvedBy}"
            };

            _logger.LogInformation("Recorded approval for deployment {DeploymentId}, stage {Stage} by {ApprovedBy}",
                deploymentId, stage, approvedBy);

            return Task.FromResult(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to record approval for deployment {DeploymentId}, stage {Stage}",
                deploymentId, stage);
            return Task.FromResult(false);
        }
    }

    /// <inheritdoc />
    public Task<ApprovalStatus?> GetApprovalStatusAsync(
        string deploymentId,
        DeploymentStage stage,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (_approvals.ContainsKey(deploymentId) && _approvals[deploymentId].ContainsKey(stage))
            {
                return Task.FromResult<ApprovalStatus?>(_approvals[deploymentId][stage]);
            }
            return Task.FromResult<ApprovalStatus?>(null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get approval status for deployment {DeploymentId}, stage {Stage}",
                deploymentId, stage);
            return Task.FromResult<ApprovalStatus?>(null);
        }
    }

    private async Task ExecuteStageLogicAsync(
        DeploymentContext context,
        DeploymentStage stage,
        StageConfiguration configuration,
        StageExecutionResult result,
        CancellationToken cancellationToken)
    {
        // Simulate stage-specific execution time
        var delay = stage switch
        {
            DeploymentStage.Validation => 1000,
            DeploymentStage.Build => 5000,
            DeploymentStage.Test => 3000,
            DeploymentStage.Security => 2000,
            DeploymentStage.Staging => 4000,
            DeploymentStage.UAT => 1500,
            DeploymentStage.Canary => 3000,
            DeploymentStage.Production => 6000,
            DeploymentStage.Verification => 2000,
            DeploymentStage.Cleanup => 500,
            _ => 1000
        };

        await Task.Delay(delay, cancellationToken);

        // Add stage-specific logs
        result.Logs.Add($"Starting {stage} stage");
        result.Logs.Add($"Executing {stage} operations for {context.ArtifactName} v{context.Version}");
        result.Logs.Add($"Completed {stage} stage successfully");

        // Add stage-specific outputs
        result.Outputs[$"{stage.ToString().ToLower()}_completed"] = true;
        result.Outputs[$"{stage.ToString().ToLower()}_timestamp"] = DateTimeOffset.UtcNow;
    }

    private async Task RunValidationTestsAsync(
        List<ValidationTest> tests,
        StageExecutionResult result,
        CancellationToken cancellationToken)
    {
        foreach (var test in tests)
        {
            _logger.LogDebug("Running validation test: {TestName}", test.Name);

            // Simulate test execution
            await Task.Delay(500, cancellationToken);

            // For demo purposes, randomly pass/fail non-required tests
            var passed = test.IsRequired || Random.Shared.NextDouble() > 0.1; // 90% pass rate for optional tests

            result.Logs.Add($"Validation test '{test.Name}': {(passed ? "PASSED" : "FAILED")}");

            if (!passed && test.IsRequired)
            {
                throw new InvalidOperationException($"Required validation test '{test.Name}' failed");
            }
        }
    }
}