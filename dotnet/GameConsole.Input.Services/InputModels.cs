using GameConsole.Input.Core;

namespace GameConsole.Input.Services;

/// <summary>
/// Represents a collection of input events that occurred over time.
/// Used for input analysis, prediction, and recording.
/// </summary>
public class InputHistory
{
    /// <summary>
    /// The sequence of input events in chronological order.
    /// </summary>
    public IReadOnlyList<InputEvent> Events { get; }
    
    /// <summary>
    /// The time window this history covers.
    /// </summary>
    public TimeSpan TimeWindow { get; }
    
    /// <summary>
    /// Maximum number of events to keep in history.
    /// </summary>
    public int MaxEvents { get; }
    
    private readonly List<InputEvent> _events;
    
    /// <summary>
    /// Initializes a new instance of the InputHistory class.
    /// </summary>
    /// <param name="maxEvents">Maximum number of events to keep.</param>
    /// <param name="timeWindow">Maximum time window to keep events for.</param>
    public InputHistory(int maxEvents = 1000, TimeSpan timeWindow = default)
    {
        MaxEvents = maxEvents;
        TimeWindow = timeWindow == default ? TimeSpan.FromMinutes(5) : timeWindow;
        _events = new List<InputEvent>();
        Events = _events.AsReadOnly();
    }
    
    /// <summary>
    /// Adds an input event to the history.
    /// </summary>
    /// <param name="inputEvent">The event to add.</param>
    public void AddEvent(InputEvent inputEvent)
    {
        _events.Add(inputEvent);
        
        // Remove old events based on time window and max count
        var cutoffTime = DateTime.UtcNow - TimeWindow;
        while (_events.Count > 0 && (_events.Count > MaxEvents || _events[0].Timestamp < cutoffTime))
        {
            _events.RemoveAt(0);
        }
    }
    
    /// <summary>
    /// Clears all events from the history.
    /// </summary>
    public void Clear()
    {
        _events.Clear();
    }
}

/// <summary>
/// Represents a prediction of what input might occur next.
/// </summary>
public class InputPrediction
{
    /// <summary>
    /// The predicted input event.
    /// </summary>
    public InputEvent PredictedEvent { get; }
    
    /// <summary>
    /// Confidence level of the prediction (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; }
    
    /// <summary>
    /// The time when this input is predicted to occur.
    /// </summary>
    public DateTime PredictedTime { get; }
    
    /// <summary>
    /// Initializes a new instance of the InputPrediction class.
    /// </summary>
    /// <param name="predictedEvent">The predicted input event.</param>
    /// <param name="confidence">Confidence level (0.0 to 1.0).</param>
    /// <param name="predictedTime">When the input is predicted to occur.</param>
    public InputPrediction(InputEvent predictedEvent, float confidence, DateTime predictedTime)
    {
        PredictedEvent = predictedEvent ?? throw new ArgumentNullException(nameof(predictedEvent));
        Confidence = Math.Max(0.0f, Math.Min(confidence, 1.0f));
        PredictedTime = predictedTime;
    }
}

/// <summary>
/// Represents a suggested input completion.
/// </summary>
public class InputSuggestion
{
    /// <summary>
    /// The suggested input sequence.
    /// </summary>
    public IReadOnlyList<InputEvent> SuggestedSequence { get; }
    
    /// <summary>
    /// Confidence level of the suggestion (0.0 to 1.0).
    /// </summary>
    public float Confidence { get; }
    
    /// <summary>
    /// Human-readable description of the suggestion.
    /// </summary>
    public string Description { get; }
    
    /// <summary>
    /// Initializes a new instance of the InputSuggestion class.
    /// </summary>
    /// <param name="suggestedSequence">The suggested input sequence.</param>
    /// <param name="confidence">Confidence level (0.0 to 1.0).</param>
    /// <param name="description">Description of the suggestion.</param>
    public InputSuggestion(IEnumerable<InputEvent> suggestedSequence, float confidence, string description)
    {
        SuggestedSequence = suggestedSequence?.ToList().AsReadOnly() ?? throw new ArgumentNullException(nameof(suggestedSequence));
        Confidence = Math.Max(0.0f, Math.Min(confidence, 1.0f));
        Description = description ?? throw new ArgumentNullException(nameof(description));
    }
}

/// <summary>
/// Represents a recorded sequence of input events.
/// </summary>
public class InputSequence
{
    /// <summary>
    /// Name of the input sequence.
    /// </summary>
    public string Name { get; }
    
    /// <summary>
    /// The recorded input events.
    /// </summary>
    public IReadOnlyList<InputEvent> Events { get; }
    
    /// <summary>
    /// When the recording was created.
    /// </summary>
    public DateTime CreatedAt { get; }
    
    /// <summary>
    /// Duration of the recorded sequence.
    /// </summary>
    public TimeSpan Duration { get; }
    
    /// <summary>
    /// Initializes a new instance of the InputSequence class.
    /// </summary>
    /// <param name="name">Name of the sequence.</param>
    /// <param name="events">The recorded events.</param>
    /// <param name="createdAt">When the recording was created.</param>
    public InputSequence(string name, IEnumerable<InputEvent> events, DateTime createdAt)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        var eventList = events?.ToList() ?? throw new ArgumentNullException(nameof(events));
        Events = eventList.AsReadOnly();
        CreatedAt = createdAt;
        
        if (eventList.Count > 0)
        {
            Duration = eventList.Max(e => e.Timestamp) - eventList.Min(e => e.Timestamp);
        }
        else
        {
            Duration = TimeSpan.Zero;
        }
    }
}

/// <summary>
/// Represents input mapping configuration for customizable key bindings.
/// </summary>
public class InputMappingConfiguration
{
    /// <summary>
    /// Dictionary mapping physical inputs to logical actions.
    /// </summary>
    public IReadOnlyDictionary<string, string> Mappings { get; }
    
    /// <summary>
    /// Name of this configuration profile.
    /// </summary>
    public string ProfileName { get; }
    
    /// <summary>
    /// When this configuration was last modified.
    /// </summary>
    public DateTime LastModified { get; }
    
    private readonly Dictionary<string, string> _mappings;
    
    /// <summary>
    /// Initializes a new instance of the InputMappingConfiguration class.
    /// </summary>
    /// <param name="profileName">Name of the profile.</param>
    /// <param name="mappings">Initial mappings.</param>
    public InputMappingConfiguration(string profileName, Dictionary<string, string>? mappings = null)
    {
        ProfileName = profileName ?? throw new ArgumentNullException(nameof(profileName));
        _mappings = mappings ?? new Dictionary<string, string>();
        Mappings = new System.Collections.ObjectModel.ReadOnlyDictionary<string, string>(_mappings);
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Adds or updates a mapping.
    /// </summary>
    /// <param name="physicalInput">The physical input.</param>
    /// <param name="logicalAction">The logical action.</param>
    public void SetMapping(string physicalInput, string logicalAction)
    {
        _mappings[physicalInput] = logicalAction;
    }
    
    /// <summary>
    /// Removes a mapping.
    /// </summary>
    /// <param name="physicalInput">The physical input to unmap.</param>
    public void RemoveMapping(string physicalInput)
    {
        _mappings.Remove(physicalInput);
    }
    
    /// <summary>
    /// Gets the logical action mapped to a physical input.
    /// </summary>
    /// <param name="physicalInput">The physical input.</param>
    /// <returns>The mapped logical action, or null if not mapped.</returns>
    public string? GetMapping(string physicalInput)
    {
        return _mappings.TryGetValue(physicalInput, out var action) ? action : null;
    }
}