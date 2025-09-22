using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core.Agents;

/// <summary>
/// Context for dialogue generation including conversation history.
/// </summary>
public class DialogueContext
{
    /// <summary>
    /// Gets or sets the character or NPC identifier.
    /// </summary>
    public string CharacterId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current conversation state.
    /// </summary>
    public ConversationState ConversationState { get; set; } = new ConversationState();

    /// <summary>
    /// Gets or sets the conversation history.
    /// </summary>
    public DialogueEntry[] History { get; set; } = Array.Empty<DialogueEntry>();

    /// <summary>
    /// Gets or sets relevant game context variables.
    /// </summary>
    public Dictionary<string, object> GameContext { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Brief requirements for quest generation.
/// </summary>
public class QuestBrief
{
    /// <summary>
    /// Gets or sets the quest type or category.
    /// </summary>
    public string QuestType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the target difficulty level.
    /// </summary>
    public string DifficultyLevel { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets required story elements or themes.
    /// </summary>
    public string[] RequiredElements { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets constraints or limitations for the quest.
    /// </summary>
    public string[] Constraints { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the target completion time in minutes.
    /// </summary>
    public int TargetCompletionTime { get; set; }
}

/// <summary>
/// Detailed quest outline with objectives and narrative.
/// </summary>
public class QuestOutline
{
    /// <summary>
    /// Gets or sets the quest title.
    /// </summary>
    public string Title { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the quest description and background.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the main quest objectives.
    /// </summary>
    public QuestObjective[] Objectives { get; set; } = Array.Empty<QuestObjective>();

    /// <summary>
    /// Gets or sets the narrative beats and story progression.
    /// </summary>
    public NarrativeBeat[] NarrativeBeats { get; set; } = Array.Empty<NarrativeBeat>();

    /// <summary>
    /// Gets or sets the estimated completion time.
    /// </summary>
    public TimeSpan EstimatedDuration { get; set; }
}

/// <summary>
/// Context for story and narrative generation.
/// </summary>
public class StoryContext
{
    /// <summary>
    /// Gets or sets the narrative type or genre.
    /// </summary>
    public string NarrativeType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current story state and variables.
    /// </summary>
    public Dictionary<string, object> StoryVariables { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the characters involved in this story segment.
    /// </summary>
    public Character[] Characters { get; set; } = Array.Empty<Character>();

    /// <summary>
    /// Gets or sets the target tone or mood.
    /// </summary>
    public string Tone { get; set; } = string.Empty;
}

/// <summary>
/// A node in a dialogue tree structure.
/// </summary>
public class DialogueNode
{
    /// <summary>
    /// Gets or sets the unique identifier for this dialogue node.
    /// </summary>
    public string NodeId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the dialogue text for this node.
    /// </summary>
    public string Text { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the character speaking this dialogue.
    /// </summary>
    public string Speaker { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the possible player response options.
    /// </summary>
    public DialogueOption[] Options { get; set; } = Array.Empty<DialogueOption>();

    /// <summary>
    /// Gets or sets conditions that must be met to reach this node.
    /// </summary>
    public string[] Conditions { get; set; } = Array.Empty<string>();
}

/// <summary>
/// A complete dialogue tree with multiple interconnected nodes.
/// </summary>
public class DialogueTree
{
    /// <summary>
    /// Gets or sets the root node of the dialogue tree.
    /// </summary>
    public DialogueNode? RootNode { get; set; }

    /// <summary>
    /// Gets or sets all nodes in the dialogue tree.
    /// </summary>
    public Dictionary<string, DialogueNode> Nodes { get; set; } = new Dictionary<string, DialogueNode>();

    /// <summary>
    /// Gets or sets metadata about the dialogue tree.
    /// </summary>
    public DialogueMetadata Metadata { get; set; } = new DialogueMetadata();
}

/// <summary>
/// Supporting classes for dialogue system.
/// </summary>
public class ConversationState
{
    public string CurrentTopic { get; set; } = string.Empty;
    public string Mood { get; set; } = string.Empty;
    public Dictionary<string, int> RelationshipScores { get; set; } = new Dictionary<string, int>();
}

public class DialogueEntry
{
    public string Speaker { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}

public class QuestObjective
{
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

public class NarrativeBeat
{
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int Order { get; set; }
}

public class Character
{
    public string CharacterId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, object> Traits { get; set; } = new Dictionary<string, object>();
}

public class DialogueOption
{
    public string OptionId { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;
    public string NextNodeId { get; set; } = string.Empty;
    public string[] Requirements { get; set; } = Array.Empty<string>();
}

public class DialogueMetadata
{
    public string Title { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
    public int EstimatedMinutes { get; set; }
}