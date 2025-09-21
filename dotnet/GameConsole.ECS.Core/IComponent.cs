namespace GameConsole.ECS.Core;

/// <summary>
/// Marker interface for all components in the ECS system.
/// Components are pure data containers that define entity behavior and state.
/// </summary>
/// <remarks>
/// Components should be simple data structures (structs or classes) that contain
/// only data, no behavior. All logic should be implemented in systems that process
/// components. This enables data-oriented design and optimal memory layout.
/// </remarks>
public interface IComponent
{
    // Pure marker interface - components are data-only
}