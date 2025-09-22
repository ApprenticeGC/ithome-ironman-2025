using GameConsole.Core.Abstractions;

namespace GameConsole.UI.Core;

/// <summary>
/// Core UI service interface for the GameConsole UI system.
/// Provides unified interface for console-based UI operations.
/// </summary>
public interface IService : GameConsole.Core.Abstractions.IService
{
    #region UI Operations
    
    /// <summary>
    /// Renders all UI components to the console.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RenderAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Clears the entire console screen.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task ClearScreenAsync(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Adds a UI component to the root level.
    /// </summary>
    /// <param name="component">The component to add.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task AddComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Removes a UI component from the root level.
    /// </summary>
    /// <param name="component">The component to remove.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task RemoveComponentAsync(IUIComponent component, CancellationToken cancellationToken = default);
    
    #endregion

    #region Capability Access
    
    /// <summary>
    /// Gets the console renderer capability.
    /// </summary>
    IUIRenderer? Renderer { get; }
    
    /// <summary>
    /// Gets the text component manager capability.
    /// </summary>
    ITextComponentCapability? TextComponents { get; }
    
    #endregion
    
    #region Console Properties
    
    /// <summary>
    /// Gets the current console window size.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The console window size.</returns>
    Task<Size> GetConsoleSize(CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a value indicating whether the console supports colors.
    /// </summary>
    bool SupportsColors { get; }
    
    #endregion
}

/// <summary>
/// Capability interface for text component management operations.
/// </summary>
public interface ITextComponentCapability : ICapabilityProvider
{
    /// <summary>
    /// Creates a new text component.
    /// </summary>
    /// <param name="id">Unique identifier for the component.</param>
    /// <param name="text">Initial text content.</param>
    /// <param name="position">Initial position.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>The created text component.</returns>
    Task<ITextComponent> CreateTextComponentAsync(string id, string text, Position position, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Updates the text content of a component.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="newText">The new text content.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task UpdateTextAsync(ITextComponent component, string newText, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Sets the colors for a text component.
    /// </summary>
    /// <param name="component">The component to update.</param>
    /// <param name="colors">The new colors.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetColorsAsync(ITextComponent component, ConsoleColor colors, CancellationToken cancellationToken = default);
}