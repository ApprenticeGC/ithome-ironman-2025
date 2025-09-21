using Microsoft.Extensions.Logging;
using Xunit;
using GameConsole.AI.Clustering.Models;
using GameConsole.AI.Clustering.Services;
using GameConsole.AI.Clustering.Interfaces;

namespace GameConsole.AI.Clustering.Tests;

/// <summary>
/// Integration tests demonstrating the complete AI clustering system working together.
/// </summary>
public class AIClusteringIntegrationTests : IDisposable
{
    private readonly ILogger<AIClusterManager> _clusterLogger;
    private readonly ILogger<AINodeManager> _nodeLogger;
    private readonly ILogger<ClusterAIRouter> _routerLogger;
    private readonly ILogger<AIClusterMonitor> _monitorLogger;
    
    private readonly AIClusterManager _clusterManager;
    private readonly AINodeManager _nodeManager;
    private readonly ClusterAIRouter _router;
    private readonly AIClusterMonitor _monitor;

    public AIClusteringIntegrationTests()
    {
        using var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
        
        _clusterLogger = loggerFactory.CreateLogger<AIClusterManager>();
        _nodeLogger = loggerFactory.CreateLogger<AINodeManager>();
        _routerLogger = loggerFactory.CreateLogger<ClusterAIRouter>();
        _monitorLogger = loggerFactory.CreateLogger<AIClusterMonitor>();
        
        _clusterManager = new AIClusterManager(_clusterLogger);
        _nodeManager = new AINodeManager(_nodeLogger);
        _router = new ClusterAIRouter(_routerLogger, _clusterManager);
        _monitor = new AIClusterMonitor(_monitorLogger, _clusterManager, _nodeManager);
    }

    [Fact]
    public async Task Complete_Clustering_Workflow_Should_Work()
    {
        // Step 1: Initialize all services
        await _clusterManager.InitializeAsync();
        await _nodeManager.InitializeAsync();
        await _router.InitializeAsync();
        await _monitor.InitializeAsync();

        // Step 2: Start all services
        await _clusterManager.StartAsync();
        await _nodeManager.StartAsync();
        await _router.StartAsync();
        await _monitor.StartAsync();

        Assert.True(_clusterManager.IsRunning);
        Assert.True(_nodeManager.IsRunning);
        Assert.True(_router.IsRunning);
        Assert.True(_monitor.IsRunning);

        // Step 3: Configure the cluster
        var config = new ClusterConfiguration
        {
            ClusterName = "integration-test-cluster",
            SeedNodes = new[] { "akka.tcp://integration-test-cluster@127.0.0.1:8080" },
            MinimumNodes = 1,
            MaximumNodes = 5,
            AutoScaling = new AutoScalingConfiguration { Enabled = true }
        };

        await _clusterManager.InitializeClusterAsync(config);

        // Step 4: Initialize a node with AI capabilities
        var capabilities = new[]
        {
            new AgentCapability
            {
                CapabilityId = "dialogue-agent",
                AgentType = "dialogue",
                SupportedOperations = new[] { "chat", "conversation" },
                Performance = new CapabilityPerformance
                {
                    ExpectedLatencyMs = 200,
                    MaxThroughputPerSecond = 50,
                    QualityScore = 0.95
                },
                Resources = new ResourceRequirements
                {
                    MinCpuCores = 2.0,
                    MinMemoryMb = 1024
                }
            },
            new AgentCapability
            {
                CapabilityId = "analysis-agent",
                AgentType = "analysis",
                SupportedOperations = new[] { "analyze", "summarize" },
                Performance = new CapabilityPerformance
                {
                    ExpectedLatencyMs = 500,
                    MaxThroughputPerSecond = 20,
                    QualityScore = 0.90
                },
                Resources = new ResourceRequirements
                {
                    MinCpuCores = 4.0,
                    MinMemoryMb = 2048
                }
            }
        };

        await _nodeManager.InitializeNodeAsync("integration-node-1", "127.0.0.1", 8080, capabilities);

        // Step 5: Add the node to the cluster
        await _clusterManager.AddNodeAsync(_nodeManager.CurrentNode);

        // Step 6: Verify cluster state
        var nodes = await _clusterManager.GetClusterNodesAsync();
        Assert.Single(nodes);
        Assert.Equal("integration-node-1", nodes[0].NodeId);
        Assert.Equal(2, nodes[0].Capabilities.Count);

        // Step 7: Test routing functionality
        var routingRequest = new RoutingRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            AgentType = "dialogue",
            Operation = "chat",
            MessagePayload = "Hello, AI cluster!",
            Priority = 1
        };

        var routingDecision = await _router.RouteMessageAsync(routingRequest);
        
        Assert.NotNull(routingDecision);
        Assert.NotNull(routingDecision.SelectedNode);
        Assert.Equal("integration-node-1", routingDecision.SelectedNode.NodeId);

        // Step 8: Test monitoring functionality
        var clusterHealth = await _monitor.GetClusterHealthAsync();
        
        Assert.Equal(ClusterStatus.Healthy, clusterHealth.Status);
        Assert.Equal(1, clusterHealth.TotalNodes);
        Assert.Equal(1, clusterHealth.HealthyNodes);
        Assert.Equal(2, clusterHealth.Metrics.ActiveAgents);

        // Step 9: Test scaling recommendation
        var scalingRecommendation = await _monitor.GetScalingRecommendationAsync();
        
        Assert.NotNull(scalingRecommendation);
        Assert.InRange(scalingRecommendation.Confidence, 0.0, 1.0);

        // Step 10: Test routing statistics
        var routingStats = await _router.GetRoutingStatisticsAsync();
        
        Assert.True(routingStats.TotalRequests > 0);
        Assert.True(routingStats.SuccessfulRoutes > 0);

        // Step 11: Clean shutdown
        await _monitor.StopAsync();
        await _router.StopAsync();
        await _nodeManager.StopAsync();
        await _clusterManager.StopAsync();

        Assert.False(_monitor.IsRunning);
        Assert.False(_router.IsRunning);
        Assert.False(_nodeManager.IsRunning);
        Assert.False(_clusterManager.IsRunning);
    }

    [Fact]
    public async Task Multi_Node_Cluster_Should_Work()
    {
        // Initialize services
        await _clusterManager.InitializeAsync();
        await _nodeManager.InitializeAsync();
        await _router.InitializeAsync();
        await _monitor.InitializeAsync();
        
        await _clusterManager.StartAsync();
        await _nodeManager.StartAsync();
        await _router.StartAsync();
        await _monitor.StartAsync();

        // Configure cluster
        var config = new ClusterConfiguration
        {
            ClusterName = "multi-node-cluster",
            SeedNodes = new[] { "akka.tcp://multi-node-cluster@127.0.0.1:8080" },
            MinimumNodes = 2,
            MaximumNodes = 10,
            AutoScaling = new AutoScalingConfiguration { Enabled = true }
        };

        await _clusterManager.InitializeClusterAsync(config);

        // Create multiple nodes with different capabilities
        var node1 = new ClusterNode
        {
            NodeId = "node-1",
            Address = "127.0.0.1",
            Port = 8081,
            Capabilities = new[]
            {
                new AgentCapability
                {
                    CapabilityId = "dialogue-high-performance",
                    AgentType = "dialogue",
                    SupportedOperations = new[] { "chat", "conversation" },
                    Performance = new CapabilityPerformance
                    {
                        ExpectedLatencyMs = 100,
                        MaxThroughputPerSecond = 100,
                        QualityScore = 0.98
                    },
                    Resources = new ResourceRequirements
                    {
                        MinCpuCores = 4.0,
                        MinMemoryMb = 2048
                    }
                }
            },
            Health = NodeHealth.Healthy
        };

        var node2 = new ClusterNode
        {
            NodeId = "node-2",
            Address = "127.0.0.1",
            Port = 8082,
            Capabilities = new[]
            {
                new AgentCapability
                {
                    CapabilityId = "analysis-specialized",
                    AgentType = "analysis",
                    SupportedOperations = new[] { "analyze", "summarize", "classify" },
                    Performance = new CapabilityPerformance
                    {
                        ExpectedLatencyMs = 300,
                        MaxThroughputPerSecond = 30,
                        QualityScore = 0.95
                    },
                    Resources = new ResourceRequirements
                    {
                        MinCpuCores = 8.0,
                        MinMemoryMb = 4096
                    }
                }
            },
            Health = NodeHealth.Healthy
        };

        // Add nodes to cluster
        await _clusterManager.AddNodeAsync(node1);
        await _clusterManager.AddNodeAsync(node2);

        // Verify cluster has both nodes
        var nodes = await _clusterManager.GetClusterNodesAsync();
        Assert.Equal(2, nodes.Count);

        // Test routing to different node types
        var dialogueRequest = new RoutingRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            AgentType = "dialogue",
            Operation = "chat",
            MessagePayload = "Route to dialogue node"
        };

        var analysisRequest = new RoutingRequest
        {
            RequestId = Guid.NewGuid().ToString(),
            AgentType = "analysis",
            Operation = "analyze",
            MessagePayload = "Route to analysis node"
        };

        var dialogueDecision = await _router.RouteMessageAsync(dialogueRequest);
        var analysisDecision = await _router.RouteMessageAsync(analysisRequest);

        Assert.Equal("node-1", dialogueDecision.SelectedNode?.NodeId);
        Assert.Equal("node-2", analysisDecision.SelectedNode?.NodeId);

        // Test cluster health with multiple nodes
        var health = await _monitor.GetClusterHealthAsync();
        Assert.Equal(2, health.TotalNodes);
        Assert.Equal(2, health.HealthyNodes);
        Assert.Equal(ClusterStatus.Healthy, health.Status);

        // Clean up
        await _monitor.StopAsync();
        await _router.StopAsync();
        await _nodeManager.StopAsync();
        await _clusterManager.StopAsync();
    }

    public void Dispose()
    {
        _monitor?.DisposeAsync().AsTask().Wait();
        _router?.DisposeAsync().AsTask().Wait();
        _nodeManager?.DisposeAsync().AsTask().Wait();
        _clusterManager?.DisposeAsync().AsTask().Wait();
    }
}