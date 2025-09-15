# RFC-007: AI Agent Integration

- **Start Date**: 2025-01-15
- **RFC Author**: Claude
- **Status**: Draft
- **Depends On**: RFC-001, RFC-002, RFC-004

## Summary

This RFC defines the integration of AI agents into the GameConsole architecture. The system supports multiple AI backends (Ollama, OpenAI, Azure), different agent types (DirectorAgent, DialogueAgent, CodexAgent), and both Editor and Runtime AI profiles with adaptive token budgets and capability-based extensions.

## Motivation

GameConsole needs AI integration that provides:

1. **Adaptive PCG**: AI-directed procedural content generation beyond static algorithms
2. **Contextual Assistance**: Player help, quest generation, lore queries
3. **Development Tools**: Asset analysis, dialogue writing, scene optimization
4. **Multi-Backend Support**: Local (Ollama) and cloud (OpenAI/Azure) AI services
5. **Profile-Based Behavior**: Different AI capabilities for Editor vs Runtime scenarios
6. **Engine Integration**: AI enhancements for Unity, Godot, and custom engines

AI agents should integrate seamlessly with the existing service architecture while providing extensible capabilities.

## Detailed Design

### AI Service Category (Tier 1+2)

#### Core AI Service Contract
```csharp
// GameConsole.AI.Core/src/Services/IService.cs
namespace GameConsole.AI.Services;

/// <summary>
/// Tier 1: AI orchestration service interface (pure .NET, async-first).
/// Manages AI agent lifecycle and routing between different AI capabilities.
/// </summary>
public interface IService : GameConsole.Services.IService, ICapabilityProvider
{
    Task InitializeAsync(AIProfile profile, CancellationToken ct = default);
    Task ShutdownAsync(CancellationToken ct = default);

    // Core agent orchestration
    Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken ct = default);
    Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken ct = default);

    // Agent discovery and management
    IEnumerable<string> GetAvailableAgents();
    Task<AgentMetadata> GetAgentInfoAsync(string agentId, CancellationToken ct = default);

    // Context management
    Task<string> CreateConversationAsync(string agentId, CancellationToken ct = default);
    Task<bool> EndConversationAsync(string conversationId, CancellationToken ct = default);
}

/// <summary>
/// AI profile configuration for different usage scenarios
/// </summary>
public class AIProfile
{
    public TaskKind TaskKind { get; set; }
    public string BaseUrl { get; set; } = "http://localhost:11434/v1";
    public string Model { get; set; } = "llama3.1:8b";
    public int MaxTokens { get; set; } = 256;
    public float Temperature { get; set; } = 0.6f;
    public int MaxParallel { get; set; } = 2;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(5);
    public string[] EnabledTools { get; set; } = Array.Empty<string>();
    public bool UseRemoteGateway { get; set; } = false;
    public string? GatewayHost { get; set; }
    public int GatewayPort { get; set; } = 4053;
}

public enum TaskKind
{
    EditorAuthoring,    // Long context, rich tools, higher latency OK
    EditorAnalysis,     // Asset QA, import validation, batch processing
    RuntimeDirector,    // Tight budget, low latency, PCG steering
    RuntimeCodex        // Player help, lore queries, safe subset only
}
```

#### Specialized Agent Contracts
```csharp
// Procedural content generation director
public interface IAgentDirector : ICapabilityProvider
{
    Task<EncounterIntent> GetEncounterIntentAsync(PlayerSnapshot snapshot, CancellationToken ct = default);
    Task<string> GetFlavorTextAsync(GameContext context, CancellationToken ct = default);
    Task<DungeonParameters> AdaptDungeonAsync(PlayerProgress progress, CancellationToken ct = default);
    Task<LootTable> GenerateLootTableAsync(EncounterContext context, CancellationToken ct = default);
}

// Dialogue and narrative generation
public interface IDialogueAgent : ICapabilityProvider
{
    Task<string> GenerateResponseAsync(DialogueContext context, CancellationToken ct = default);
    Task<QuestOutline> DraftQuestAsync(QuestBrief brief, CancellationToken ct = default);
    Task<string> GenerateNarrativeAsync(StoryContext context, CancellationToken ct = default);
    Task<DialogueTree> ExpandDialogueTreeAsync(DialogueNode node, CancellationToken ct = default);
}

// Player assistance and knowledge base
public interface ICodexAgent : ICapabilityProvider
{
    Task<string> QueryLoreAsync(string question, GameWorld world, CancellationToken ct = default);
    Task<string> GetHintAsync(PlayerState state, CancellationToken ct = default);
    Task<string> ExplainMechanicAsync(string mechanic, CancellationToken ct = default);
    Task<QuestGuidance> GetQuestGuidanceAsync(ActiveQuest quest, CancellationToken ct = default);
}
```

#### AI-Enhanced Service Extensions
```csharp
// Audio service AI extensions
namespace GameConsole.Audio.Extensions.AI;

public interface IAudioDirectorCapability : ICapabilityProvider
{
    Task<AudioDirective> GetAudioDirectiveAsync(GameContext context, CancellationToken ct = default);
    Task<SoundscapeDefinition> GenerateSoundscapeAsync(EnvironmentContext environment, CancellationToken ct = default);
    Task<MusicTransition> AdaptMusicAsync(PlayerState state, EmotionalContext emotion, CancellationToken ct = default);
}

// Input service AI extensions
namespace GameConsole.Input.Extensions.AI;

public interface IInputPredictionCapability : ICapabilityProvider
{
    Task<InputPrediction> PredictNextInputAsync(InputHistory history, CancellationToken ct = default);
    Task<PlayerIntent> DetectPlayerIntentAsync(InputSequence sequence, CancellationToken ct = default);
    Task<bool> IsStuckPatternDetectedAsync(InputHistory history, CancellationToken ct = default);
}
```

### AI Agent Provider Implementations (Tier 4)

#### Akka.NET AI Orchestrator Provider
```csharp
// providers/ai/Akka.AI.Provider/AkkaAIOrchestrator.cs
[ProviderFor(typeof(GameConsole.AI.Services.IService))]
[RequiresGate("HAS_AI")]
[RequiresGate("HAS_AKKA")]
public class AkkaAIOrchestrator : GameConsole.AI.Services.IService, ICapabilityProvider
{
    private ActorSystem? _system;
    private IActorRef? _coordinator;
    private AIProfile _profile = new();

    public string ServiceId => "akka.ai.orchestrator";
    public int Priority => 100;
    public double StartupTimeMs => 300.0;

    public async Task StartAsync()
    {
        await InitializeAsync(_profile);
    }

    public async Task InitializeAsync(AIProfile profile, CancellationToken ct = default)
    {
        _profile = profile;

        // Create Akka actor system
        _system = ActorSystem.Create("AIOrchestrator", CreateAkkaConfig(profile));

        // Create LLM worker pool
        var llmPool = _system.ActorOf(Props.Create(() =>
            new LlmWorkerPoolActor(profile)), "llm-pool");

        // Create tool bus for agent tools
        var toolBus = _system.ActorOf(Props.Create(() =>
            new ToolBusActor()), "tool-bus");

        // Create conversation coordinator
        _coordinator = _system.ActorOf(Props.Create(() =>
            new ConversationCoordinatorActor(llmPool, toolBus)), "coordinator");

        await WarmupAgents(ct);
    }

    public async Task<string> InvokeAgentAsync(string agentId, string input, CancellationToken ct = default)
    {
        if (_coordinator == null) throw new InvalidOperationException("AI orchestrator not initialized");

        var conversationId = Guid.NewGuid().ToString();
        var response = await _coordinator.Ask<AgentResponse>(
            new AgentRequest(conversationId, agentId, input),
            TimeSpan.FromMilliseconds(_profile.Timeout.TotalMilliseconds),
            ct);

        return response.Text;
    }

    public async Task<IAsyncEnumerable<string>> StreamAgentAsync(string agentId, string input, CancellationToken ct = default)
    {
        if (_coordinator == null) throw new InvalidOperationException("AI orchestrator not initialized");

        var conversationId = Guid.NewGuid().ToString();
        var streamRequest = new StreamAgentRequest(conversationId, agentId, input);

        return _coordinator.Ask<IAsyncEnumerable<string>>(streamRequest, ct);
    }

    public bool TryGetCapability<T>(out T? capability) where T : class
    {
        // Akka orchestrator provides rich AI capabilities
        capability = this as T;
        return capability != null;
    }

    private Config CreateAkkaConfig(AIProfile profile)
    {
        var hocon = $@"
            akka {{
                actor {{
                    provider = local
                    serializers {{
                        hyperion = ""Akka.Serialization.HyperionSerializer, Akka.Serialization.Hyperion""
                    }}
                    serialization-bindings {{
                        ""System.Object"" = hyperion
                    }}
                }}
                ai {{
                    base-url = ""{profile.BaseUrl}""
                    model = ""{profile.Model}""
                    max-tokens = {profile.MaxTokens}
                    temperature = {profile.Temperature}
                    max-parallel = {profile.MaxParallel}
                    timeout = ""{profile.Timeout.TotalMilliseconds}ms""
                }}
            }}";

        return ConfigurationFactory.ParseString(hocon);
    }
}
```

#### LLM Worker Pool Actor
```csharp
// Akka actor for handling LLM requests with concurrency control
public sealed class LlmWorkerPoolActor : ReceiveActor
{
    private readonly AIProfile _profile;
    private readonly HttpClient _httpClient;
    private readonly SemaphoreSlim _concurrencyGate;

    public LlmWorkerPoolActor(AIProfile profile)
    {
        _profile = profile;
        _httpClient = new HttpClient { BaseAddress = new Uri(profile.BaseUrl) };
        _concurrencyGate = new SemaphoreSlim(profile.MaxParallel);

        ReceiveAsync<LlmRequest>(HandleRequestAsync);
    }

    private async Task HandleRequestAsync(LlmRequest request)
    {
        await _concurrencyGate.WaitAsync();

        try
        {
            var payload = new
            {
                model = _profile.Model,
                messages = new[] { new { role = "user", content = request.Input } },
                max_tokens = _profile.MaxTokens,
                temperature = _profile.Temperature,
                stream = request.Stream
            };

            using var response = await _httpClient.PostAsJsonAsync("/chat/completions", payload);
            response.EnsureSuccessStatusCode();

            if (request.Stream)
            {
                await HandleStreamingResponseAsync(request, response);
            }
            else
            {
                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var content = jsonResponse.GetProperty("choices")[0]
                    .GetProperty("message")
                    .GetProperty("content")
                    .GetString() ?? "";

                Sender.Tell(new LlmResponse(request.ConversationId, request.AgentId, content));
            }
        }
        catch (Exception ex)
        {
            Sender.Tell(new LlmError(request.ConversationId, request.AgentId, ex.Message));
        }
        finally
        {
            _concurrencyGate.Release();
        }
    }

    private async Task HandleStreamingResponseAsync(LlmRequest request, HttpResponseMessage response)
    {
        using var stream = await response.Content.ReadAsStreamAsync();
        using var reader = new StreamReader(stream);

        var buffer = new StringBuilder();
        string? line;

        while ((line = await reader.ReadLineAsync()) != null)
        {
            if (line.StartsWith("data: "))
            {
                var jsonData = line["data: ".Length..];
                if (jsonData == "[DONE]") break;

                try
                {
                    var tokenData = JsonSerializer.Deserialize<JsonElement>(jsonData);
                    var delta = tokenData.GetProperty("choices")[0].GetProperty("delta");

                    if (delta.TryGetProperty("content", out var content))
                    {
                        var token = content.GetString();
                        if (!string.IsNullOrEmpty(token))
                        {
                            buffer.Append(token);
                            Sender.Tell(new LlmToken(request.ConversationId, request.AgentId, token));
                        }
                    }
                }
                catch (JsonException)
                {
                    // Skip malformed JSON chunks
                }
            }
        }

        Sender.Tell(new LlmComplete(request.ConversationId, request.AgentId, buffer.ToString()));
    }
}
```

### AI Profile System

#### Profile-Based Service Configuration
```csharp
// GameConsole.Profiles.AI/AIProfiles.cs
public static class AIProfiles
{
    public static AIProfile EditorAuthoring => new()
    {
        TaskKind = TaskKind.EditorAuthoring,
        MaxTokens = 1024,           // Rich content generation
        Temperature = 0.8f,         // Creative responses
        MaxParallel = 4,           // More concurrent requests
        Timeout = TimeSpan.FromSeconds(15),
        EnabledTools = new[] { "web.fetch", "file.read", "git.diff", "asset.analyze" }
    };

    public static AIProfile EditorAnalysis => new()
    {
        TaskKind = TaskKind.EditorAnalysis,
        MaxTokens = 512,           // Analysis summaries
        Temperature = 0.3f,        // Factual, consistent
        MaxParallel = 6,          // Batch processing
        Timeout = TimeSpan.FromSeconds(10),
        EnabledTools = new[] { "asset.validate", "import.check", "performance.analyze" }
    };

    public static AIProfile RuntimeDirector => new()
    {
        TaskKind = TaskKind.RuntimeDirector,
        MaxTokens = 128,           // Quick decisions
        Temperature = 0.6f,        // Balanced creativity
        MaxParallel = 2,          // Limited resources
        Timeout = TimeSpan.FromSeconds(3),
        EnabledTools = new[] { "pcg.hint", "difficulty.adjust" }
    };

    public static AIProfile RuntimeCodex => new()
    {
        TaskKind = TaskKind.RuntimeCodex,
        MaxTokens = 256,           // Player help responses
        Temperature = 0.4f,        // Helpful, accurate
        MaxParallel = 1,          // Single query at a time
        Timeout = TimeSpan.FromSeconds(5),
        EnabledTools = new[] { "lore.query", "hint.generate" }
    };
}
```

### AI-Enhanced Engine Providers

#### Unity Engine with AI Capabilities
```csharp
// providers/engines/Unity.Engine.Provider/UnityAIGameEngine.cs
[ProviderFor(typeof(IGameEngine))]
[ProviderFor(typeof(IAgentDirector))]
[RequiresGate("HAS_UNITY")]
[RequiresGate("HAS_AI")]
public class UnityAIGameEngine : IGameEngine, IAgentDirector, ICapabilityProvider
{
    private readonly GameConsole.AI.Services.IService _aiService;
    private readonly ILogger<UnityAIGameEngine> _logger;

    public UnityAIGameEngine(GameConsole.AI.Services.IService aiService, ILogger<UnityAIGameEngine> logger)
    {
        _aiService = aiService;
        _logger = logger;
    }

    // IGameEngine implementation with Unity simulation
    public async Task StartAsync(GameConfig config, CancellationToken ct = default)
    {
        await InitializeUnitySimulation(config, ct);
        await InitializeAIIntegration(config.AIProfile, ct);
    }

    // IAgentDirector implementation with Unity-specific context
    public async Task<EncounterIntent> GetEncounterIntentAsync(PlayerSnapshot snapshot, CancellationToken ct = default)
    {
        // Unity-specific context: NavMesh analysis, Physics queries, Lighting conditions
        var unityContext = await AnalyzeUnitySceneAsync(snapshot.Position, ct);
        var enrichedSnapshot = EnrichWithUnityData(snapshot, unityContext);

        var prompt = BuildEncounterPrompt(enrichedSnapshot, unityContext);
        var response = await _aiService.InvokeAgentAsync("director", prompt, ct);

        return ParseEncounterIntent(response);
    }

    public async Task<string> GetFlavorTextAsync(GameContext context, CancellationToken ct = default)
    {
        // Unity-specific: Asset references, Material properties, Shader effects
        var visualContext = await ExtractUnityVisualContext(context, ct);
        var prompt = $"Unity scene: {visualContext}\nGenerate atmospheric flavor text (2-3 sentences):";

        return await _aiService.InvokeAgentAsync("narrator", prompt, ct);
    }

    public bool TryGetCapability<T>(out T? capability) where T : class
    {
        // Unity provides rich AI capabilities with engine integration
        capability = this as T;
        return capability != null;
    }

    private async Task<UnitySceneContext> AnalyzeUnitySceneAsync(Vector3 position, CancellationToken ct)
    {
        // Simulate Unity-specific scene analysis
        return new UnitySceneContext
        {
            NavigationMeshDensity = await QueryNavMeshDensity(position, ct),
            NearbyObjects = await FindNearbyGameObjects(position, 10f, ct),
            LightingConditions = await AnalyzeLighting(position, ct),
            PhysicsComplexity = await CalculatePhysicsComplexity(position, ct)
        };
    }
}
```

### AI Tool System

#### Tool Registration and Execution
```csharp
// GameConsole.AI.Core/src/Tools/ToolBus.cs
public class ToolBus
{
    private readonly Dictionary<string, IAITool> _tools = new();
    private readonly IServiceProvider _serviceProvider;

    public void RegisterTool(string name, IAITool tool)
    {
        _tools[name] = tool;
    }

    public async Task<string> ExecuteToolAsync(string toolName, string arguments, CancellationToken ct = default)
    {
        if (!_tools.TryGetValue(toolName, out var tool))
        {
            throw new InvalidOperationException($"Tool '{toolName}' not found");
        }

        var args = JsonSerializer.Deserialize<Dictionary<string, object>>(arguments) ?? new();
        return await tool.ExecuteAsync(args, ct);
    }
}

// Example AI tools
public interface IAITool
{
    string Name { get; }
    string Description { get; }
    Task<string> ExecuteAsync(Dictionary<string, object> args, CancellationToken ct = default);
}

[Tool("web.fetch")]
public class WebFetchTool : IAITool
{
    public string Name => "web.fetch";
    public string Description => "Fetch content from a web URL";

    public async Task<string> ExecuteAsync(Dictionary<string, object> args, CancellationToken ct = default)
    {
        var url = args["url"].ToString()!;
        using var client = new HttpClient();
        var response = await client.GetStringAsync(url, ct);
        return response.Substring(0, Math.Min(2000, response.Length)); // Truncate for token limit
    }
}

[Tool("asset.analyze")]
public class AssetAnalyzeTool : IAITool
{
    public string Name => "asset.analyze";
    public string Description => "Analyze game asset properties and optimization opportunities";

    public async Task<string> ExecuteAsync(Dictionary<string, object> args, CancellationToken ct = default)
    {
        var assetPath = args["path"].ToString()!;
        // Simulate asset analysis
        return $"Asset {assetPath}: Texture resolution optimizable, UV mapping efficient, LOD available";
    }
}
```

## Benefits

### Adaptive Gameplay
- **Smart PCG**: AI-directed procedural generation goes beyond random algorithms
- **Contextual Assistance**: Player help that understands game state and history
- **Dynamic Difficulty**: AI adjusts challenge based on player performance

### Development Enhancement
- **Asset Analysis**: AI reviews assets for optimization opportunities
- **Dialogue Authoring**: Generate and expand dialogue trees with consistent character voices
- **Scene Optimization**: Analyze scenes for performance and gameplay flow issues

### Multi-Backend Flexibility
- **Local Development**: Use Ollama for zero-cost local AI during development
- **Production Scale**: Deploy with OpenAI/Azure for consistent performance
- **Hybrid Approach**: Mix local and cloud AI based on use case

### Engine Integration
- **Context Awareness**: AI agents understand engine-specific scene data
- **Capability Extension**: Engine providers gain AI features through composition
- **Performance Optimization**: Engine-specific AI optimizations

## Drawbacks

### Complexity
- **Multi-System Integration**: AI, actors, containers, and game engines
- **Token Management**: Balancing context size with response time
- **Error Handling**: AI failures must not break core gameplay

### Performance Considerations
- **Latency Sensitivity**: Runtime AI must meet strict timing requirements
- **Resource Usage**: AI operations compete with game performance
- **Network Dependencies**: Cloud AI introduces connectivity requirements

### Cost Management
- **Token Consumption**: Especially with cloud AI providers
- **Rate Limiting**: Managing API limits across multiple agents
- **Model Selection**: Balancing capability with cost and performance

## Implementation Strategy

### Phase 1: Core AI Infrastructure
- Implement Akka.NET orchestrator
- Basic agent contracts (Director, Dialogue, Codex)
- Ollama provider for local development

### Phase 2: Engine Integration
- AI-enhanced Unity and Godot providers
- Tool system for agent capabilities
- Profile-based configuration

### Phase 3: Advanced Features
- Streaming responses for real-time interaction
- Conversation context management
- Multi-agent collaboration

### Phase 4: Production Optimization
- Cloud AI providers (OpenAI, Azure)
- Performance monitoring and optimization
- Advanced tool ecosystem

## Success Metrics

- **Response Time**: Runtime agents respond within 200ms (95th percentile)
- **Context Retention**: Conversations maintain context for 10+ turns
- **Tool Usage**: 80% of agent requests successfully use tools when needed
- **Engine Integration**: AI features work seamlessly with all supported engines

## Future Possibilities

- **Multi-Agent Collaboration**: Agents that work together on complex tasks
- **Learning Systems**: Agents that adapt based on player behavior
- **Custom Model Training**: Train specialized models for game-specific tasks
- **Visual AI Integration**: Computer vision for asset and scene analysis