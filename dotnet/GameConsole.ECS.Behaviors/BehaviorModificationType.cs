namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Represents the type of modification to perform on a behavior.
/// </summary>
public enum BehaviorModificationType
{
    /// <summary>
    /// Add a new component to the behavior.
    /// </summary>
    AddComponent,

    /// <summary>
    /// Remove a component from the behavior.
    /// </summary>
    RemoveComponent,

    /// <summary>
    /// Update an existing component in the behavior.
    /// </summary>
    UpdateComponent,

    /// <summary>
    /// Replace one component with another.
    /// </summary>
    ReplaceComponent
}