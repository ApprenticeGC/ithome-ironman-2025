using Microsoft.Extensions.Logging;
using GameConsole.AI.Actors.Actors;
using GameConsole.AI.Actors.Messages;

namespace GameConsole.AI.Actors.Examples;

/// <summary>
/// Example AI agent implementation that demonstrates how to create concrete AI agents
/// using the BaseAIActor class.
/// </summary>
public class EchoAIAgent : BaseAIActor
{
    private readonly string _agentId;

    public EchoAIAgent(ILogger<EchoAIAgent> logger, string agentId = "echo-agent") 
        : base(logger)
    {
        _agentId = agentId;
        Logger.LogInformation("EchoAIAgent {AgentId} initialized", _agentId);
    }

    protected override AgentResponse ProcessInvokeAgent(InvokeAgent message)
    {
        Logger.LogInformation("Processing echo request for {Input}", message.Input);
        
        // Simulate some processing time
        Thread.Sleep(100);
        
        var response = $"Echo Agent ({_agentId}) received: {message.Input}";
        return new AgentResponse(message.AgentId, response, true);
    }

    protected override void ProcessStreamAgent(StreamAgent message)
    {
        Logger.LogInformation("Processing streaming echo request for {Input}", message.Input);
        
        // Simulate streaming response by sending multiple chunks
        var words = message.Input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        
        for (int i = 0; i < words.Length; i++)
        {
            var chunk = $"Echo[{i+1}]: {words[i]}";
            var isLast = i == words.Length - 1;
            
            Sender.Tell(new AgentStreamChunk(message.AgentId, chunk, isLast), Self);
            
            // Simulate processing delay
            if (!isLast)
            {
                Thread.Sleep(50);
            }
        }
        
        if (words.Length == 0)
        {
            // Handle empty input
            Sender.Tell(new AgentStreamChunk(message.AgentId, "Echo: (empty input)", true), Self);
        }
    }

    protected override AgentMetadata GetAgentMetadata()
    {
        return new AgentMetadata(
            Id: _agentId,
            Name: "Echo AI Agent",
            Description: "A simple AI agent that echoes back user input for testing purposes",
            Version: "1.0.0",
            Capabilities: new[] { "echo", "streaming", "testing" },
            IsAvailable: true
        );
    }
}

/// <summary>
/// Example AI agent that performs simple text analysis.
/// </summary>
public class TextAnalysisAIAgent : BaseAIActor
{
    private readonly string _agentId;

    public TextAnalysisAIAgent(ILogger<TextAnalysisAIAgent> logger, string agentId = "text-analysis-agent") 
        : base(logger)
    {
        _agentId = agentId;
        Logger.LogInformation("TextAnalysisAIAgent {AgentId} initialized", _agentId);
    }

    protected override AgentResponse ProcessInvokeAgent(InvokeAgent message)
    {
        Logger.LogInformation("Processing text analysis for {Input}", message.Input);
        
        // Simulate some processing time
        Thread.Sleep(200);
        
        var analysis = AnalyzeText(message.Input);
        return new AgentResponse(message.AgentId, analysis, true);
    }

    protected override void ProcessStreamAgent(StreamAgent message)
    {
        Logger.LogInformation("Processing streaming text analysis for {Input}", message.Input);
        
        // Stream analysis results step by step
        var input = message.Input ?? "";
        
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Analyzing text: '{input}'", false), Self);
        Thread.Sleep(100);
        
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Character count: {input.Length}", false), Self);
        Thread.Sleep(100);
        
        var words = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Word count: {words.Length}", false), Self);
        Thread.Sleep(100);
        
        var sentences = input.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Sentence count: {sentences.Length}", false), Self);
        Thread.Sleep(100);
        
        var uniqueWords = words.Select(w => w.ToLowerInvariant()).Distinct().Count();
        Sender.Tell(new AgentStreamChunk(message.AgentId, $"Unique words: {uniqueWords}", true), Self);
    }

    protected override AgentMetadata GetAgentMetadata()
    {
        return new AgentMetadata(
            Id: _agentId,
            Name: "Text Analysis AI Agent",
            Description: "An AI agent that performs basic text analysis including word count, character count, and sentence analysis",
            Version: "1.0.0",
            Capabilities: new[] { "text-analysis", "word-count", "character-count", "sentence-analysis", "streaming" },
            IsAvailable: true
        );
    }

    private string AnalyzeText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return "Analysis: Empty or whitespace-only input";
        }

        var words = input.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
        var sentences = input.Split(new[] { '.', '!', '?' }, StringSplitOptions.RemoveEmptyEntries);
        var uniqueWords = words.Select(w => w.ToLowerInvariant()).Distinct().Count();
        
        return $"Text Analysis Results:\n" +
               $"- Character count: {input.Length}\n" +
               $"- Word count: {words.Length}\n" +
               $"- Sentence count: {sentences.Length}\n" +
               $"- Unique words: {uniqueWords}\n" +
               $"- Average words per sentence: {(sentences.Length > 0 ? (double)words.Length / sentences.Length : 0):F1}";
    }
}