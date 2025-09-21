# GameConsole Container Deployment System

This project implements RFC-012: Container-Native Deployment System, providing comprehensive container orchestration capabilities with support for Docker and Kubernetes deployments.

## Overview

The container deployment system follows the GameConsole 4-tier architecture and provides:

- **Container Orchestration**: Unified interface for managing containerized applications
- **Multiple Providers**: Support for Docker and Kubernetes deployment targets
- **Health Monitoring**: Real-time health checking and monitoring of deployed containers
- **Auto-scaling**: Automatic scaling based on demand and resource utilization
- **Blue-Green Deployments**: Zero-downtime deployment strategies
- **Service Mesh Integration**: Built-in support for service mesh configurations

## Architecture

### Core Components

- **IContainerOrchestrator**: Main service interface for deployment management
- **ContainerOrchestratorService**: Orchestrates multiple deployment providers
- **IDeploymentProvider**: Base interface for container deployment providers
- **IHealthMonitorProvider**: Interface for health monitoring services

### Provider Implementations

- **DockerDeploymentProvider**: Docker container deployment and management
- **KubernetesDeploymentProvider**: Kubernetes orchestration and scaling
- **ContainerHealthMonitor**: Health monitoring with Observable streams

### Capability Interfaces

- **IContainerHealthMonitoring**: Real-time health status updates
- **IBlueGreenDeployment**: Blue-green deployment orchestration
- **IServiceMeshIntegration**: Service mesh configuration and metrics

## Usage

### Basic Deployment

```csharp
// Create deployment configuration
var deployment = new DeploymentConfiguration
{
    Id = "my-app",
    Name = "my-application",
    Image = "nginx",
    Tag = "1.21",
    Replicas = 3,
    Ports = new[]
    {
        new PortMapping { ContainerPort = 80, HostPort = 8080 }
    },
    Environment = new Dictionary<string, string>
    {
        { "ENV", "production" }
    }
};

// Deploy using the orchestrator
var result = await orchestrator.DeployAsync(deployment);
if (result.Success)
{
    Console.WriteLine($"Deployed successfully: {result.DeploymentId}");
    foreach (var endpoint in result.Endpoints)
    {
        Console.WriteLine($"Service available at: {endpoint.Url}");
    }
}
```

### Health Monitoring

```csharp
// Configure health monitoring
var healthMonitor = serviceProvider.GetService<ContainerHealthMonitor>();

// Set up health check configuration
var healthConfig = new HealthCheckConfiguration
{
    Command = "curl -f http://localhost/health",
    Interval = TimeSpan.FromSeconds(30),
    Timeout = TimeSpan.FromSeconds(5),
    FailureThreshold = 3
};

await healthMonitor.ConfigureHealthCheckAsync(deploymentId, healthConfig);

// Monitor health status
var healthStream = healthMonitor.MonitorHealthAsync(deploymentId);
healthStream.Subscribe(status => 
{
    Console.WriteLine($"Health Status: {status.Status} - {status.Message}");
});
```

### Scaling Operations

```csharp
// Scale deployment to 5 replicas
var scaleResult = await orchestrator.ScaleAsync(deploymentId, 5);
if (scaleResult.Success)
{
    Console.WriteLine($"Scaled from {scaleResult.PreviousReplicas} to {scaleResult.CurrentReplicas} replicas");
}
```

### Service Registration

Services are automatically registered using the `ServiceAttribute`:

```csharp
[Service("MyDeploymentProvider", "1.0.0", "Custom deployment provider", 
    Lifetime = ServiceLifetime.Singleton, 
    Categories = new[] { "Deployment" })]
public class MyDeploymentProvider : IDeploymentProvider
{
    // Implementation
}
```

## Configuration Models

### DeploymentConfiguration

- **Basic Settings**: ID, Name, Image, Tag, Replicas
- **Network Configuration**: Port mappings and protocols
- **Resource Management**: CPU and memory limits/requests  
- **Storage**: Volume mounts and persistent storage
- **Environment**: Environment variables and configuration
- **Health Checks**: Health check configuration
- **Labels**: Metadata and labeling

### Resource Management

```csharp
Resources = new ResourceConfiguration
{
    CpuLimit = 1000,      // 1 CPU core
    CpuRequest = 500,     // 0.5 CPU cores
    MemoryLimit = 1024 * 1024 * 1024,  // 1GB
    MemoryRequest = 512 * 1024 * 1024  // 512MB
}
```

## Provider Features

### Docker Provider
- Container lifecycle management
- Automated image building and deployment
- Port mapping and volume mounting
- Resource limits and health checks
- Log streaming and monitoring

### Kubernetes Provider  
- Deployment and ReplicaSet management
- Service discovery and load balancing
- Auto-scaling and rolling updates
- ConfigMaps and Secrets integration
- Helm chart support (planned)

## Testing

The project includes comprehensive unit tests covering:

- Provider functionality and validation
- Deployment scenarios and error handling
- Health monitoring and Observable streams
- Service lifecycle management
- Configuration validation

Run tests with:
```bash
dotnet test GameConsole.Deployment.Containers.Tests/
```

## Dependencies

- **GameConsole.Core.Abstractions**: Base service interfaces
- **Microsoft.Extensions.Logging**: Logging infrastructure
- **System.Reactive**: Observable streams for health monitoring

## Integration

Register the deployment system with your service container:

```csharp
services.RegisterFromAttributes(typeof(ContainerOrchestratorService).Assembly);

// Or manually register specific services
services.RegisterSingleton<IContainerOrchestrator, ContainerOrchestratorService>();
services.RegisterSingleton<IDeploymentProvider, DockerDeploymentProvider>();
services.RegisterSingleton<IDeploymentProvider, KubernetesDeploymentProvider>();
services.RegisterSingleton<IHealthMonitorProvider, ContainerHealthMonitor>();
```

## Future Enhancements

- **Helm Chart Support**: Deploy complex applications using Helm
- **Service Mesh Integration**: Full Istio/Linkerd integration  
- **Advanced Scheduling**: Node affinity and pod disruption budgets
- **Multi-Cloud Support**: AWS ECS/EKS, Azure Container Instances
- **GitOps Integration**: Automated deployments from Git repositories
- **Observability**: Metrics collection and distributed tracing

## Contributing

Follow the existing GameConsole patterns:
- Use the 4-tier architecture for new features
- Include comprehensive unit tests
- Follow the service registration patterns
- Document new capabilities and interfaces