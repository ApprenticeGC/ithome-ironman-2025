namespace TestLib.Game;

/// <summary>
/// Represents a basic game component that can be updated and managed.
/// </summary>
public interface IGameComponent
{
    /// <summary>
    /// Gets the name of the component.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets or sets whether the component is active.
    /// </summary>
    bool IsActive { get; set; }

    /// <summary>
    /// Updates the component state.
    /// </summary>
    void Update();
}