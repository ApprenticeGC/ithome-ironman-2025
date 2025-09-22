namespace GameConsole.ECS.Core;

/// <summary>
/// Marker interface for all ECS components.
/// Components are data containers that define the behavior and state of entities.
/// Implement this interface on structs for optimal performance.
/// </summary>
public interface IComponent
{
    // Marker interface - no members required
    // Components should be implemented as structs containing only data
}