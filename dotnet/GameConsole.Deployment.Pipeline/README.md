# GameConsole Deployment Pipeline

This library implements RFC-012-02 deployment pipeline automation for the GameConsole system, providing comprehensive CI/CD integration with rollback capabilities.

## Architecture Overview

The deployment pipeline follows the GameConsole 4-tier architecture:

- **Tier 1**: Core interfaces (`IDeploymentPipeline`, `IDeploymentProvider`, `IRollbackManager`, etc.)
- **Tier 2**: Service proxies and generated code (handled by existing infrastructure)
- **Tier 3**: Business logic and orchestration (`DeploymentPipeline`, `DeploymentStageManager`, `RollbackManager`)
- **Tier 4**: Platform integrations (`CIPipelineProvider` with GitHub Actions, Azure DevOps, Jenkins adapters)

## Key Features

### 🚀 **Automated CI/CD Pipeline Integration**
- Support for GitHub Actions, Azure DevOps, and Jenkins
- Unified workflow triggering and monitoring across platforms
- Platform-specific adapter pattern for extensibility

### 📊 **Stage-based Deployment Management**
- Sequential stage execution with dependency management
- Manual approval gates for critical deployments
- Health check validation at each stage
- Comprehensive status tracking and reporting

### 🔄 **Automatic Rollback Capabilities**
- Configurable rollback triggers (health check failures, error rates)
- Version history management for precise rollback targeting
- Automatic and manual rollback scenarios
- Rollback validation and safety checks

### 📈 **Deployment Monitoring & Metrics**
- Real-time deployment status tracking
- Performance metrics collection (duration, success rates, resource usage)
- Event-driven status notifications
- Deployment progress reporting

### 🔧 **Health Check Integration**
- Configurable health check endpoints
- Retry logic with exponential backoff
- Success criteria validation
- Integration with deployment stage validation

## Core Interfaces

### IDeploymentPipeline
Main orchestration interface for deployment operations:
```csharp
Task<DeploymentResult> ExecutePipelineAsync(DeploymentConfig config, CancellationToken cancellationToken = default);
Task<RollbackResult> RollbackAsync(RollbackConfig config, CancellationToken cancellationToken = default);
Task<DeploymentStatus> GetDeploymentStatusAsync(string deploymentId, CancellationToken cancellationToken = default);
```

### IDeploymentProvider  
Abstraction for CI/CD platform integrations:
```csharp
Task<WorkflowResult> TriggerWorkflowAsync(WorkflowConfig config, CancellationToken cancellationToken = default);
Task<WorkflowStatus> GetWorkflowStatusAsync(string workflowId, CancellationToken cancellationToken = default);
```

### IRollbackManager
Deployment recovery and version management:
```csharp
Task<RollbackResult> RollbackAsync(string deploymentId, string reason, CancellationToken cancellationToken = default);
Task<IReadOnlyCollection<DeploymentVersion>> GetRollbackOptionsAsync(string environment, CancellationToken cancellationToken = default);
```

## Usage Example

```csharp
// 1. Set up dependencies (typically via DI container)
var pluginLifecycleManager = serviceProvider.GetRequiredService<IPluginLifecycleManager>();
var rollbackManager = new RollbackManager();
var cipipelineProvider = new CIPipelineProvider();

// 2. Create deployment pipeline
var pipeline = new DeploymentPipeline(pluginLifecycleManager, rollbackManager, cipipelineProvider);
await pipeline.InitializeAsync();
await pipeline.StartAsync();

// 3. Configure deployment
var config = new DeploymentConfig
{
    DeploymentId = "deploy-2025-01",
    Name = "Production Deployment",
    Version = "2.0.0",
    TargetEnvironment = "production",
    Stages = new List<StageConfig>
    {
        new StageConfig
        {
            Id = "dev-deploy",
            Name = "Development",
            TargetEnvironment = "development",
            WorkflowConfig = new WorkflowConfig
            {
                ProviderType = "GitHubActions",
                WorkflowId = "deploy-workflow",
                Repository = "company/app",
                Reference = "main"
            }
        }
    },
    RollbackConfig = new RollbackConfig
    {
        EnableAutoRollback = true,
        Triggers = new RollbackTriggers
        {
            OnHealthCheckFailure = true,
            ErrorRateThreshold = 0.05
        }
    }
};

// 4. Execute deployment
var result = await pipeline.ExecutePipelineAsync(config);
```

## Configuration Models

### DeploymentConfig
Primary configuration for deployment operations:
- `DeploymentId`: Unique identifier for the deployment
- `Stages`: List of deployment stages to execute
- `RollbackConfig`: Automatic rollback configuration
- `Timeout`: Overall deployment timeout

### StageConfig  
Configuration for individual deployment stages:
- `RequiresApproval`: Whether manual approval is needed
- `WorkflowConfig`: CI/CD workflow configuration
- `HealthCheck`: Health check validation settings
- `TargetEnvironment`: Environment for this stage

### WorkflowConfig
CI/CD platform workflow configuration:
- `ProviderType`: Platform type (GitHubActions, AzureDevOps, Jenkins)
- `WorkflowId`: Workflow identifier on the platform
- `Repository`: Source repository
- `Reference`: Branch/tag to deploy

## Integration Points

### Plugin Lifecycle Integration
The deployment pipeline integrates with the existing plugin lifecycle management system to:
- Coordinate plugin deployments
- Handle plugin dependencies during deployment
- Manage plugin state transitions

### Event System
Comprehensive event system for monitoring:
- `DeploymentStatusChangedEventArgs`: Overall deployment status changes
- `StageStatusChangedEventArgs`: Individual stage status changes  
- `RollbackStatusChangedEventArgs`: Rollback operation status changes

### Health Monitoring
Integrated health checking system:
- HTTP endpoint validation
- Custom validation rules
- Configurable retry logic
- Success criteria evaluation

## Testing

The library includes comprehensive unit tests (49 tests, 100% pass rate):
- `DeploymentPipelineTests`: Main orchestrator tests
- `CIPipelineProviderTests`: CI/CD provider integration tests
- `RollbackManagerTests`: Rollback functionality tests

Run tests:
```bash
dotnet test GameConsole.Deployment.Pipeline.Tests
```

## Extension Points

### Custom CI/CD Providers
Implement `ICIPlatformAdapter` for additional CI/CD platforms:
```csharp
internal class CustomCIAdapter : ICIPlatformAdapter
{
    public Task<WorkflowResult> TriggerWorkflowAsync(WorkflowConfig config, CancellationToken cancellationToken)
    {
        // Custom platform integration
    }
}
```

### Custom Deployment Stages
Implement `IDeploymentStage` for specialized deployment steps:
```csharp
public class CustomDeploymentStage : IDeploymentStage
{
    public Task<StageResult> ExecuteAsync(StageConfig config, CancellationToken cancellationToken)
    {
        // Custom deployment logic
    }
}
```

## Dependencies

- **GameConsole.Core.Abstractions**: Base service interfaces
- **GameConsole.Plugins.Lifecycle**: Plugin lifecycle management (RFC-006-01 dependency)
- **Microsoft.Extensions.DependencyInjection**: For service registration
- **.NET 8.0**: Target framework

## Future Enhancements

- Container orchestration integration (Kubernetes, Docker Swarm)
- Infrastructure as Code support (Terraform, ARM templates)
- Advanced deployment strategies (Blue/Green, Canary)
- Integration with monitoring systems (Prometheus, Grafana)
- Deployment analytics and reporting dashboard