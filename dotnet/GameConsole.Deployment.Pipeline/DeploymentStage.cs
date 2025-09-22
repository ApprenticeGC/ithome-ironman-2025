namespace GameConsole.Deployment.Pipeline;

/// <summary>
/// Represents the various stages in a deployment pipeline.
/// </summary>
public enum DeploymentStage
{
    /// <summary>
    /// Initial validation and preparation stage.
    /// </summary>
    Validation,

    /// <summary>
    /// Building artifacts and dependencies.
    /// </summary>
    Build,

    /// <summary>
    /// Running automated tests.
    /// </summary>
    Test,

    /// <summary>
    /// Security scanning and compliance checks.
    /// </summary>
    Security,

    /// <summary>
    /// Deployment to staging environment.
    /// </summary>
    Staging,

    /// <summary>
    /// User acceptance testing in staging.
    /// </summary>
    UAT,

    /// <summary>
    /// Canary deployment to a subset of production.
    /// </summary>
    Canary,

    /// <summary>
    /// Full production deployment.
    /// </summary>
    Production,

    /// <summary>
    /// Post-deployment verification and monitoring.
    /// </summary>
    Verification,

    /// <summary>
    /// Cleanup and finalization.
    /// </summary>
    Cleanup
}