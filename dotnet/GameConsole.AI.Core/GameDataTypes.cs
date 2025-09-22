namespace GameConsole.AI.Services;

/// <summary>
/// Represents player state information for AI decision making.
/// </summary>
public class PlayerSnapshot
{
    /// <summary>
    /// Gets or sets the player's current level.
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the player's current health points.
    /// </summary>
    public int Health { get; set; }

    /// <summary>
    /// Gets or sets the player's maximum health points.
    /// </summary>
    public int MaxHealth { get; set; }

    /// <summary>
    /// Gets or sets the player's equipped items.
    /// </summary>
    public string[] Equipment { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the player's current location.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets additional player attributes.
    /// </summary>
    public Dictionary<string, object> Attributes { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents player progress information for content adaptation.
/// </summary>
public class PlayerProgress
{
    /// <summary>
    /// Gets or sets completed quests.
    /// </summary>
    public string[] CompletedQuests { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets discovered locations.
    /// </summary>
    public string[] DiscoveredLocations { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets defeated enemies.
    /// </summary>
    public string[] DefeatedEnemies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets playtime in hours.
    /// </summary>
    public double PlaytimeHours { get; set; }

    /// <summary>
    /// Gets or sets difficulty preferences.
    /// </summary>
    public string DifficultyPreference { get; set; } = "Normal";
}

/// <summary>
/// Represents encounter intent for procedural content generation.
/// </summary>
public class EncounterIntent
{
    /// <summary>
    /// Gets or sets the type of encounter.
    /// </summary>
    public string EncounterType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the difficulty level of the encounter.
    /// </summary>
    public string Difficulty { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets suggested enemies for the encounter.
    /// </summary>
    public string[] SuggestedEnemies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the narrative theme for the encounter.
    /// </summary>
    public string Theme { get; set; } = string.Empty;
}

/// <summary>
/// Represents game context for content generation.
/// </summary>
public class GameContext
{
    /// <summary>
    /// Gets or sets the current scene or area.
    /// </summary>
    public string Scene { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time of day in the game world.
    /// </summary>
    public string TimeOfDay { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the current weather conditions.
    /// </summary>
    public string Weather { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets active events in the game world.
    /// </summary>
    public string[] ActiveEvents { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets contextual metadata.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
}

/// <summary>
/// Represents dungeon generation parameters.
/// </summary>
public class DungeonParameters
{
    /// <summary>
    /// Gets or sets the size category of the dungeon.
    /// </summary>
    public string Size { get; set; } = "Medium";

    /// <summary>
    /// Gets or sets the theme or style of the dungeon.
    /// </summary>
    public string Theme { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the number of rooms to generate.
    /// </summary>
    public int RoomCount { get; set; } = 10;

    /// <summary>
    /// Gets or sets the hazard types to include.
    /// </summary>
    public string[] HazardTypes { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets special features to include.
    /// </summary>
    public string[] SpecialFeatures { get; set; } = Array.Empty<string>();
}

/// <summary>
/// Represents encounter context for loot generation.
/// </summary>
public class EncounterContext
{
    /// <summary>
    /// Gets or sets the type of encounter.
    /// </summary>
    public string EncounterType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the enemies involved in the encounter.
    /// </summary>
    public string[] Enemies { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets the location of the encounter.
    /// </summary>
    public string Location { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the player level for appropriate loot scaling.
    /// </summary>
    public int PlayerLevel { get; set; }
}

/// <summary>
/// Represents a loot table for encounter rewards.
/// </summary>
public class LootTable
{
    /// <summary>
    /// Gets or sets guaranteed items from the encounter.
    /// </summary>
    public string[] GuaranteedItems { get; set; } = Array.Empty<string>();

    /// <summary>
    /// Gets or sets possible random items and their drop chances.
    /// </summary>
    public Dictionary<string, float> RandomItems { get; set; } = new Dictionary<string, float>();

    /// <summary>
    /// Gets or sets the currency amount to reward.
    /// </summary>
    public int CurrencyAmount { get; set; }

    /// <summary>
    /// Gets or sets experience points to award.
    /// </summary>
    public int ExperiencePoints { get; set; }
}