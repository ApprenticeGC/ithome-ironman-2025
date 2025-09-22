using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using GameConsole.AI.Core;
using GameConsole.AI.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace GameConsole.AI.Services.Tests
{
    /// <summary>
    /// Integration tests for AI Agent Actor Clustering functionality.
    /// Tests the core features implemented in GAME-RFC-009-03.
    /// </summary>
    public class AIAgentClusteringIntegrationTests
    {
        private readonly Mock<ILogger<AkkaAIOrchestrationService>> _mockLogger;
        private readonly Mock<ILogger<AIAgentActor>> _mockActorLogger;

        public AIAgentClusteringIntegrationTests()
        {
            _mockLogger = new Mock<ILogger<AkkaAIOrchestrationService>>();
            _mockActorLogger = new Mock<ILogger<AIAgentActor>>();
        }

        [Fact]
        public async Task OrchestrationService_Should_Initialize_Successfully()
        {
            // Arrange
            var service = new AkkaAIOrchestrationService(_mockLogger.Object);

            // Act
            await service.InitializeAsync();

            // Assert
            Assert.False(service.IsRunning);
            
            // Start service
            await service.StartAsync();
            Assert.True(service.IsRunning);
            
            // Cleanup
            await service.StopAsync();
            Assert.False(service.IsRunning);
            await service.DisposeAsync();
        }

        [Fact]
        public async Task Should_Create_And_Manage_AI_Agents()
        {
            // Arrange
            var service = new AkkaAIOrchestrationService(_mockLogger.Object);
            await service.InitializeAsync();
            await service.StartAsync();

            var agentConfig = new Dictionary<string, object>
            {
                ["AgentId"] = "test-agent-1",
                ["Name"] = "Test Agent 1"
            };

            try
            {
                // Act - Create agent
                var agent = await service.CreateAgentAsync("TestAgent", agentConfig);

                // Assert - Agent created successfully
                Assert.NotNull(agent);
                Assert.Equal("test-agent-1", agent.AgentId);
                Assert.Equal("Test Agent 1", agent.Name);

                // Act - Retrieve agent
                var retrievedAgent = await service.GetAgentAsync("test-agent-1");
                Assert.NotNull(retrievedAgent);
                Assert.Equal(agent.AgentId, retrievedAgent.AgentId);

                // Act - Get all agents
                var allAgents = await service.GetAllAgentsAsync();
                Assert.Single(allAgents);

                // Act - Remove agent
                var removeResult = await service.RemoveAgentAsync("test-agent-1");
                Assert.True(removeResult);

                // Verify agent removed
                var removedAgent = await service.GetAgentAsync("test-agent-1");
                Assert.Null(removedAgent);
            }
            finally
            {
                // Cleanup
                await service.StopAsync();
                await service.DisposeAsync();
            }
        }

        [Fact]
        public async Task Should_Route_Messages_To_Agents()
        {
            // Arrange
            var service = new AkkaAIOrchestrationService(_mockLogger.Object);
            await service.InitializeAsync();
            await service.StartAsync();

            var agentConfig = new Dictionary<string, object>
            {
                ["AgentId"] = "test-agent-2",
                ["Name"] = "Test Agent 2"
            };

            try
            {
                // Act - Create agent
                var agent = await service.CreateAgentAsync("TestAgent", agentConfig);

                // Create test message
                var message = new AIAgentMessage(
                    Guid.NewGuid().ToString(),
                    "Hello, AI Agent!",
                    "TestMessage",
                    DateTime.UtcNow
                );

                // Act - Route message to specific agent
                var responses = await service.RouteMessageAsync(message, "test-agent-2");

                // Assert
                Assert.Single(responses);
                var response = responses.FirstOrDefault();
                Assert.NotNull(response);
                Assert.Equal(message.MessageId, response.MessageId);
                Assert.Equal("test-agent-2", response.AgentId);
                Assert.True(response.Success);
                Assert.Contains("Test Agent 2 processed: Hello, AI Agent!", response.Content);
            }
            finally
            {
                // Cleanup
                await service.StopAsync();
                await service.DisposeAsync();
            }
        }

        [Fact]
        public async Task Should_Provide_Health_Status()
        {
            // Arrange
            var service = new AkkaAIOrchestrationService(_mockLogger.Object);
            await service.InitializeAsync();
            await service.StartAsync();

            try
            {
                // Act - Get health status with no agents
                var healthStatus = await service.GetHealthStatusAsync();

                // Assert
                Assert.NotNull(healthStatus);
                Assert.True(healthStatus.IsHealthy); // No agents means no errors
                Assert.Equal(0, healthStatus.TotalAgents);
                Assert.Equal(0, healthStatus.ActiveAgents);
                Assert.Equal(0, healthStatus.ErroredAgents);
                Assert.True(healthStatus.SystemUptime.TotalMilliseconds >= 0);
                Assert.NotNull(healthStatus.SystemMetrics);

                // Create an agent and check health again
                var agentConfig = new Dictionary<string, object>
                {
                    ["AgentId"] = "health-test-agent",
                    ["Name"] = "Health Test Agent"
                };

                await service.CreateAgentAsync("TestAgent", agentConfig);

                // Give the agent time to initialize
                await Task.Delay(100);

                var healthStatusWithAgent = await service.GetHealthStatusAsync();

                Assert.Equal(1, healthStatusWithAgent.TotalAgents);
                Assert.True(healthStatusWithAgent.ActiveAgents >= 0);
            }
            finally
            {
                // Cleanup
                await service.StopAsync();
                await service.DisposeAsync();
            }
        }

        [Fact]
        public async Task Agent_Should_Process_Messages_And_Provide_Status()
        {
            // Arrange
            var service = new AkkaAIOrchestrationService(_mockLogger.Object);
            await service.InitializeAsync();
            await service.StartAsync();

            var agentConfig = new Dictionary<string, object>
            {
                ["AgentId"] = "status-test-agent",
                ["Name"] = "Status Test Agent"
            };

            try
            {
                // Act - Create agent
                var agent = await service.CreateAgentAsync("TestAgent", agentConfig);

                // Give the agent time to initialize
                await Task.Delay(100);

                // Act - Get agent status
                var status = await agent.GetStatusAsync();

                // Assert - Check initial status
                Assert.NotNull(status);
                Assert.Equal("status-test-agent", status.AgentId);
                Assert.Equal("Status Test Agent", status.Name);
                Assert.True(status.State == AIAgentState.Idle || status.State == AIAgentState.Initializing);
                Assert.NotNull(status.Metrics);

                // Act - Send message to agent
                var message = new AIAgentMessage(
                    Guid.NewGuid().ToString(),
                    "Test message for status",
                    "StatusTest",
                    DateTime.UtcNow
                );

                var response = await agent.ProcessMessageAsync(message);

                // Assert - Check response
                Assert.NotNull(response);
                Assert.True(response.Success);
                Assert.Equal(message.MessageId, response.MessageId);
                Assert.Equal("status-test-agent", response.AgentId);

                // Act - Get updated status
                var updatedStatus = await agent.GetStatusAsync();

                // Assert - Status should show message was processed
                Assert.NotNull(updatedStatus);
                Assert.True(updatedStatus.State == AIAgentState.Idle); // Should return to idle after processing
            }
            finally
            {
                // Cleanup
                await service.StopAsync();
                await service.DisposeAsync();
            }
        }

        [Fact]
        public void AI_Models_Should_Be_Properly_Constructed()
        {
            // Arrange & Act
            var message = new AIAgentMessage("msg-1", "test content", "TestType", DateTime.UtcNow);
            var response = new AIAgentResponse("msg-1", "agent-1", "response content", AIResponseType.Information, DateTime.UtcNow, true);
            var status = new AIAgentStatus("agent-1", "Test Agent", AIAgentState.Idle, DateTime.UtcNow, null, TimeSpan.FromMinutes(1), new Dictionary<string, object>());
            var task = new AIDistributedTask("task-1", "TestTask", new Dictionary<string, object>(), TimeSpan.FromSeconds(30));
            var result = new AICoordinationResult("task-1", true, new List<string> { "agent-1" }, new Dictionary<string, object>(), TimeSpan.FromMilliseconds(100));

            // Assert
            Assert.Equal("msg-1", message.MessageId);
            Assert.Equal("test content", message.Content);
            
            Assert.Equal("msg-1", response.MessageId);
            Assert.Equal("agent-1", response.AgentId);
            Assert.True(response.Success);
            
            Assert.Equal("agent-1", status.AgentId);
            Assert.Equal(AIAgentState.Idle, status.State);
            
            Assert.Equal("task-1", task.TaskId);
            Assert.Equal(TimeSpan.FromSeconds(30), task.Timeout);
            
            Assert.Equal("task-1", result.TaskId);
            Assert.True(result.Success);
        }
    }
}