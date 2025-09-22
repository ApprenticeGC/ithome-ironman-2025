using System;
using System.Collections.Generic;

namespace GameConsole.AI.Core.Agents;

/// <summary>
/// Represents a player's current state and progress for AI decision making.
/// </summary>
public class PlayerSnapshot
{
    /// <summary>
    /// Gets or sets the player's current level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the player's skill ratings in various areas.
    /// </summary>
    public Dictionary<string, int> Skills { get; set; } = new Dictionary<string, int>();

    /// <summary>
    /// Gets or sets the player's recent performance metrics.
    /// </summary>
    public PerformanceMetrics Performance { get; set; } = new PerformanceMetrics();

    /// <summary>
    /// Gets or sets the player's current equipment and inventory.
    /// </summary>
    public InventorySnapshot Inventory { get; set; } = new InventorySnapshot();
}

/// <summary>
/// Describes the intended difficulty and type of encounter.
/// </summary>
public class EncounterIntent
{
    /// <summary>
    /// Gets or sets the recommended difficulty level (0.0 to 1.0).
    /// </summary>
    public float DifficultyLevel { get; set; }

    /// <summary>
    /// Gets or sets the type of encounter to generate.
    /// </summary>
    public string EncounterType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets specific encounter parameters.
    /// </summary>
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents the current game context for AI content generation.
/// </summary>
public class GameContext
{
    /// <summary>
    /// Gets or sets the current scene or location identifier.
    /// </summary>
    public string SceneId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current game state variables.
    /// </summary>
    public Dictionary<string, object> StateVariables { get; set; } = new Dictionary<string, object>();

    /// <summary>
    /// Gets or sets the recent game events that may affect content generation.
    /// </summary>
    public GameEvent[] RecentEvents { get; set; } = Array.Empty<GameEvent>();
}

/// <summary>
/// Represents player progress and statistics.
/// </summary>
public class PlayerProgress
{
    /// <summary>
    /// Gets or sets completed quests or achievements.
    /// </summary>
    public string[] CompletedQuests { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the total play time.
    /// </summary>
    public TimeSpan TotalPlayTime { get; set; }

    /// <summary>
    /// Gets or sets death or failure counts by category.
    /// </summary>
    public Dictionary<string, int> FailureCounts { get; set; } = new Dictionary<string, int>();
}

/// <summary>
/// Parameters for generating dungeon content.
/// </summary>
public class DungeonParameters
{
    /// <summary>
    /// Gets or sets the recommended dungeon size.
    /// </summary>
    public int Size { get; set; }

    /// <summary>
    /// Gets or sets the dungeon theme or style.
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enemy difficulty multiplier.
    /// </summary>
    public float DifficultyMultiplier { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets special features to include.
    /// </summary>
    public string[] SpecialFeatures { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Context for encounter-specific loot generation.
/// </summary>
public class EncounterContext
{
    /// <summary>
    /// Gets or sets the encounter type.
    /// </summary>
    public string EncounterType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the encounter difficulty level.
    /// </summary>
    public float DifficultyLevel { get; set; }

    /// <summary>
    /// Gets or sets the player level for appropriate loot scaling.
    /// </summary>
    public int PlayerLevel { get; set; }

    /// <summary>
    /// Gets or sets the encounter location or environment.
    /// </summary>
    public string Location { get; set; } = string.Empty;
}

/// <summary>
/// Defines loot drop probabilities and items.
/// </summary>
public class LootTable
{
    /// <summary>
    /// Gets or sets the loot items with their drop probabilities.
    /// </summary>
    public LootItem[] Items { get; set; } = Array.Empty<LootItem>();

    /// <summary>
    /// Gets or sets the guaranteed minimum drops.
    /// </summary>
    public int MinDrops { get; set; }

    /// <summary>
    /// Gets or sets the maximum possible drops.
    /// </summary>
    public int MaxDrops { get; set; }
}

/// <summary>
/// Supporting classes for player snapshot.
/// </summary>
public class PerformanceMetrics
{
    public float AverageAccuracy { get; set; }
    public float ReactionTime { get; set; }
    public int RecentDeaths { get; set; }
}

public class InventorySnapshot
{
    public string[] Items { get; set; } = Array.Empty<string>();
    public int Currency { get; set; }
}

public class GameEvent
{
    public string EventType { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
}

public class LootItem
{
    public string ItemId { get; set; } = string.Empty;
    public float DropProbability { get; set; }
    public int MinQuantity { get; set; } = 1;
    public int MaxQuantity { get; set; } = 1;
}