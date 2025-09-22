using GameConsole.Deployment.Pipeline;
using Microsoft.Extensions.Logging;

Console.WriteLine("=== GameConsole Deployment Pipeline Demo ===\n");

// Create logger factory
var loggerFactory = LoggerFactory.Create(builder => 
    builder.AddConsole().SetMinimumLevel(LogLevel.Information));

// Create deployment pipeline components
var stageManager = new DeploymentStageManager(
    loggerFactory.CreateLogger<DeploymentStageManager>());
var rollbackManager = new RollbackManager(
    loggerFactory.CreateLogger<RollbackManager>());
var ciProvider = new GitHubActionsPipelineProvider(
    loggerFactory.CreateLogger<GitHubActionsPipelineProvider>(), 
    new HttpClient());

var pipeline = new DeploymentPipeline(
    loggerFactory.CreateLogger<DeploymentPipeline>(),
    stageManager, rollbackManager, ciProvider);

// Initialize and start pipeline
await pipeline.InitializeAsync();
await pipeline.StartAsync();

Console.WriteLine("1. Creating deployment context...");
var deploymentId = Guid.NewGuid().ToString();
var context = new DeploymentContext
{
    DeploymentId = deploymentId,
    ArtifactName = "GameConsole.DemoApp",
    Version = "3.1.0",
    Environment = "staging",
    Strategy = DeploymentStrategy.Rolling,
    InitiatedBy = "demo-user",
    Configuration = new Dictionary<string, object>
    {
        ["replicas"] = 2,
        ["healthCheck"] = true,
        ["timeout"] = 300
    }
};

Console.WriteLine($"   Deployment ID: {context.DeploymentId}");
Console.WriteLine($"   Artifact: {context.ArtifactName} v{context.Version}");
Console.WriteLine($"   Environment: {context.Environment}");
Console.WriteLine($"   Strategy: {context.Strategy}");
Console.WriteLine();

// Subscribe to status change events
pipeline.DeploymentStatusChanged += (sender, args) =>
{
    Console.WriteLine($"   Status: {args.PreviousStatus} → {args.CurrentStatus} [{args.CurrentStage}]");
    if (!string.IsNullOrEmpty(args.Message))
    {
        Console.WriteLine($"   Message: {args.Message}");
    }
};

Console.WriteLine("2. Starting deployment...");
var deploymentTask = pipeline.DeployAsync(context);

// Monitor deployment progress
var lastStatus = DeploymentStatus.NotStarted;
while (!deploymentTask.IsCompleted)
{
    await Task.Delay(1000);
    
    var currentStatus = await pipeline.GetDeploymentStatusAsync(deploymentId);
    if (currentStatus?.Status != lastStatus)
    {
        lastStatus = currentStatus?.Status ?? DeploymentStatus.NotStarted;
        Console.WriteLine($"   Current Status: {lastStatus}");
    }
}

var result = await deploymentTask;

Console.WriteLine("\n3. Deployment Results:");
Console.WriteLine($"   Status: {result.Status}");
Console.WriteLine($"   Duration: {result.Duration}");
Console.WriteLine($"   Stages Completed: {result.CompletedStage}");
Console.WriteLine($"   Logs: {result.Logs.Count} entries");
Console.WriteLine($"   Metrics: {result.Metrics.Count} measurements");

if (result.Status == DeploymentStatus.Succeeded)
{
    Console.WriteLine("\n4. Testing CI/CD Provider Integration...");
    
    var ciConfig = new CIPipelineConfiguration
    {
        PipelineName = "deploy-staging",
        Repository = "gamedev/console-app",
        Branch = "main",
        EnvironmentVariables = new Dictionary<string, string>
        {
            ["ENVIRONMENT"] = "staging",
            ["VERSION"] = context.Version
        }
    };
    
    var ciResult = await ciProvider.TriggerPipelineAsync(ciConfig);
    Console.WriteLine($"   CI/CD Trigger: {(ciResult.Success ? "✅ Success" : "❌ Failed")}");
    if (!string.IsNullOrEmpty(ciResult.ErrorMessage))
    {
        Console.WriteLine($"   Error: {ciResult.ErrorMessage}");
    }
    
    Console.WriteLine("\n5. Testing Rollback Capabilities...");
    
    rollbackManager.RegisterDeployment(context);
    var canRollback = await rollbackManager.CanRollbackAsync(deploymentId);
    Console.WriteLine($"   Can Rollback: {(canRollback ? "✅ Yes" : "❌ No")}");
    
    if (canRollback)
    {
        var rollbackOptions = await rollbackManager.GetRollbackOptionsAsync(deploymentId);
        Console.WriteLine($"   Rollback Options: {rollbackOptions.Count}");
        
        foreach (var option in rollbackOptions)
        {
            Console.WriteLine($"     - {option.DisplayName} (v{option.Version}){(option.IsRecommended ? " [Recommended]" : "")}");
        }
        
        // Configure auto-rollback triggers
        var triggers = new[]
        {
            new RollbackTrigger
            {
                Type = RollbackTriggerType.ErrorRate,
                Condition = "error_rate > 5%",
                Threshold = 5.0,
                GracePeriod = TimeSpan.FromMinutes(3)
            },
            new RollbackTrigger
            {
                Type = RollbackTriggerType.ResponseTime,
                Condition = "avg_response_time > 2000ms",
                Threshold = 2000.0,
                GracePeriod = TimeSpan.FromMinutes(5)
            }
        };
        
        var triggerConfigured = await rollbackManager.ConfigureAutoRollbackAsync(deploymentId, triggers);
        Console.WriteLine($"   Auto-rollback Triggers: {(triggerConfigured ? "✅ Configured" : "❌ Failed")}");
    }
}

Console.WriteLine("\n6. Deployment History:");
var history = await pipeline.GetDeploymentHistoryAsync(limit: 5);
Console.WriteLine($"   Total Deployments: {history.Count}");

foreach (var deployment in history.Take(3))
{
    Console.WriteLine($"     - {deployment.Context.ArtifactName} v{deployment.Context.Version} → {deployment.Status}");
}

// Cleanup
await pipeline.StopAsync();
await pipeline.DisposeAsync();

Console.WriteLine("\n=== Demo Complete ===");
Console.WriteLine("The GameConsole Deployment Pipeline provides:");
Console.WriteLine("• End-to-end deployment orchestration");
Console.WriteLine("• Stage-based execution with approval gates");
Console.WriteLine("• CI/CD platform integration (GitHub Actions, Azure DevOps, Jenkins)");
Console.WriteLine("• Automatic rollback capabilities");
Console.WriteLine("• Deployment metrics and performance tracking");
Console.WriteLine("• Canary and blue-green deployment strategies");
Console.WriteLine("• Infrastructure as code support");