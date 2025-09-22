using System.Collections.Generic;
using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Implementation of IUIProfile representing a UI profile configuration.
/// </summary>
public class UIProfile : IUIProfile
{
    private readonly Dictionary<string, object> _graphicsSettings;
    private readonly Dictionary<string, object> _uiSettings;
    
    /// <inheritdoc />
    public string Id { get; }
    
    /// <inheritdoc />
    public string Name { get; private set; }
    
    /// <inheritdoc />
    public string Description { get; private set; }
    
    /// <inheritdoc />
    public string? InputProfileName { get; private set; }
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> GraphicsSettings { get; }
    
    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> UISettings { get; }
    
    /// <inheritdoc />
    public DateTime CreatedAt { get; }
    
    /// <inheritdoc />
    public DateTime LastModified { get; private set; }
    
    /// <summary>
    /// Initializes a new instance of the UIProfile class.
    /// </summary>
    /// <param name="id">Unique identifier.</param>
    /// <param name="name">Human-readable name.</param>
    /// <param name="description">Description.</param>
    /// <param name="inputProfileName">Associated input profile name.</param>
    /// <param name="graphicsSettings">Graphics settings.</param>
    /// <param name="uiSettings">UI settings.</param>
    /// <param name="createdAt">Creation time.</param>
    public UIProfile(
        string id, 
        string name, 
        string description,
        string? inputProfileName = null,
        Dictionary<string, object>? graphicsSettings = null,
        Dictionary<string, object>? uiSettings = null,
        DateTime createdAt = default)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Description = description ?? throw new ArgumentNullException(nameof(description));
        InputProfileName = inputProfileName;
        
        _graphicsSettings = graphicsSettings ?? new Dictionary<string, object>();
        _uiSettings = uiSettings ?? new Dictionary<string, object>();
        
        GraphicsSettings = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(_graphicsSettings);
        UISettings = new System.Collections.ObjectModel.ReadOnlyDictionary<string, object>(_uiSettings);
        
        CreatedAt = createdAt == default ? DateTime.UtcNow : createdAt;
        LastModified = CreatedAt;
    }
    
    /// <summary>
    /// Updates the profile's basic properties.
    /// </summary>
    /// <param name="name">New name (optional).</param>
    /// <param name="description">New description (optional).</param>
    /// <param name="inputProfileName">New input profile name (optional).</param>
    public void Update(string? name = null, string? description = null, string? inputProfileName = null)
    {
        if (!string.IsNullOrWhiteSpace(name))
            Name = name!;
        
        if (!string.IsNullOrWhiteSpace(description))
            Description = description!;
            
        if (inputProfileName != null) // Allow setting to null
            InputProfileName = inputProfileName;
            
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates a graphics setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public void SetGraphicsSetting(string key, object value)
    {
        _graphicsSettings[key] = value;
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes a graphics setting.
    /// </summary>
    /// <param name="key">Setting key to remove.</param>
    public void RemoveGraphicsSetting(string key)
    {
        _graphicsSettings.Remove(key);
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Updates a UI setting.
    /// </summary>
    /// <param name="key">Setting key.</param>
    /// <param name="value">Setting value.</param>
    public void SetUISetting(string key, object value)
    {
        _uiSettings[key] = value;
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Removes a UI setting.
    /// </summary>
    /// <param name="key">Setting key to remove.</param>
    public void RemoveUISetting(string key)
    {
        _uiSettings.Remove(key);
        LastModified = DateTime.UtcNow;
    }
    
    /// <summary>
    /// Creates a copy of this profile with a new ID.
    /// </summary>
    /// <param name="newId">New profile ID.</param>
    /// <param name="newName">New profile name (optional).</param>
    /// <returns>Cloned profile.</returns>
    public UIProfile Clone(string newId, string? newName = null)
    {
        return new UIProfile(
            newId,
            newName ?? $"{Name} (Copy)",
            Description,
            InputProfileName,
            new Dictionary<string, object>(_graphicsSettings),
            new Dictionary<string, object>(_uiSettings),
            DateTime.UtcNow);
    }
}