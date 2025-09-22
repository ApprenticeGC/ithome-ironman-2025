using System.Collections.Generic;

namespace GameConsole.Core.Abstractions;

/// <summary>
/// Represents a UI profile configuration that coordinates settings across multiple subsystems.
/// UI profiles enable switching between different interaction modes (e.g., Unity-style vs Godot-style).
/// </summary>
public interface IUIProfile
{
    /// <summary>
    /// Unique identifier for this UI profile.
    /// </summary>
    string Id { get; }
    
    /// <summary>
    /// Human-readable name for this UI profile.
    /// </summary>
    string Name { get; }
    
    /// <summary>
    /// Description of what this profile configures.
    /// </summary>
    string Description { get; }
    
    /// <summary>
    /// Input mapping profile name associated with this UI profile.
    /// </summary>
    string? InputProfileName { get; }
    
    /// <summary>
    /// Graphics settings associated with this UI profile.
    /// </summary>
    IReadOnlyDictionary<string, object> GraphicsSettings { get; }
    
    /// <summary>
    /// Additional UI-specific settings (layouts, themes, etc.).
    /// </summary>
    IReadOnlyDictionary<string, object> UISettings { get; }
    
    /// <summary>
    /// When this profile was created.
    /// </summary>
    DateTime CreatedAt { get; }
    
    /// <summary>
    /// When this profile was last modified.
    /// </summary>
    DateTime LastModified { get; }
}