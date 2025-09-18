using GameConsole.Core.Abstractions;

namespace GameConsole.Engine.Core;

/// <summary>
/// Scene transition modes that control how scenes are loaded and unloaded.
/// </summary>
public enum SceneTransitionMode
{
    /// <summary>
    /// Load the new scene additively without unloading the current scene.
    /// </summary>
    Additive,
    
    /// <summary>
    /// Replace the current scene entirely with the new scene.
    /// </summary>
    Replace,
    
    /// <summary>
    /// Load the new scene in the background while keeping the current scene active.
    /// </summary>
    Background
}

/// <summary>
/// Scene loading priority levels.
/// </summary>
public enum SceneLoadPriority
{
    /// <summary>
    /// Low priority loading that won't block the main thread.
    /// </summary>
    Low,
    
    /// <summary>
    /// Normal priority loading.
    /// </summary>
    Normal,
    
    /// <summary>
    /// High priority loading that should be completed as quickly as possible.
    /// </summary>
    High,
    
    /// <summary>
    /// Critical priority loading that blocks until complete.
    /// </summary>
    Critical
}

/// <summary>
/// Arguments for scene-related events.
/// </summary>
public class SceneEventArgs : EventArgs
{
    /// <summary>
    /// The identifier of the scene.
    /// </summary>
    public string SceneId { get; }
    
    /// <summary>
    /// Optional additional data about the scene event.
    /// </summary>
    public object? Data { get; }

    /// <summary>
    /// Initializes a new instance of the SceneEventArgs class.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene.</param>
    /// <param name="data">Optional additional data about the scene event.</param>
    public SceneEventArgs(string sceneId, object? data = null)
    {
        SceneId = sceneId ?? throw new ArgumentNullException(nameof(sceneId));
        Data = data;
    }
}

/// <summary>
/// Tier 2: Scene manager service interface for managing hierarchical scene graphs.
/// Handles scene lifecycle operations, transitions, and hierarchical scene management
/// to ensure smooth transitions without frame drops.
/// </summary>
public interface ISceneManager : IService
{
    /// <summary>
    /// Event raised when a scene starts loading.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneLoadStarted;
    
    /// <summary>
    /// Event raised when a scene completes loading.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneLoadCompleted;
    
    /// <summary>
    /// Event raised when a scene starts unloading.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneUnloadStarted;
    
    /// <summary>
    /// Event raised when a scene completes unloading.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneUnloadCompleted;
    
    /// <summary>
    /// Event raised when a scene transition begins.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneTransitionStarted;
    
    /// <summary>
    /// Event raised when a scene transition completes.
    /// </summary>
    event EventHandler<SceneEventArgs>? SceneTransitionCompleted;

    /// <summary>
    /// Gets the identifier of the currently active scene.
    /// </summary>
    string? ActiveSceneId { get; }
    
    /// <summary>
    /// Gets the identifiers of all currently loaded scenes in hierarchical order.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns scene identifiers.</returns>
    Task<IEnumerable<string>> GetLoadedScenesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a scene asynchronously with the specified priority and transition mode.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene to load.</param>
    /// <param name="mode">The transition mode for loading the scene.</param>
    /// <param name="priority">The priority level for loading the scene.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scene loading operation.</returns>
    Task LoadSceneAsync(string sceneId, SceneTransitionMode mode = SceneTransitionMode.Replace, 
        SceneLoadPriority priority = SceneLoadPriority.Normal, CancellationToken cancellationToken = default);

    /// <summary>
    /// Unloads a scene asynchronously.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene to unload.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scene unloading operation.</returns>
    Task UnloadSceneAsync(string sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Transitions from the current scene to a new scene asynchronously with smooth transition handling.
    /// </summary>
    /// <param name="toSceneId">The identifier of the scene to transition to.</param>
    /// <param name="fromSceneId">The identifier of the scene to transition from, or null for current active scene.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scene transition operation.</returns>
    Task TransitionToSceneAsync(string toSceneId, string? fromSceneId = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Preloads a scene in the background without making it active.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene to preload.</param>
    /// <param name="priority">The priority level for preloading the scene.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async scene preloading operation.</returns>
    Task PreloadSceneAsync(string sceneId, SceneLoadPriority priority = SceneLoadPriority.Low, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a scene is currently loaded.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene to check.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns true if the scene is loaded.</returns>
    Task<bool> IsSceneLoadedAsync(string sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets the active scene from among the currently loaded scenes.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene to make active.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    Task SetActiveSceneAsync(string sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the hierarchical parent scene of the specified scene.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns the parent scene identifier, or null if no parent.</returns>
    Task<string?> GetParentSceneAsync(string sceneId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the hierarchical child scenes of the specified scene.
    /// </summary>
    /// <param name="sceneId">The identifier of the scene.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation that returns child scene identifiers.</returns>
    Task<IEnumerable<string>> GetChildScenesAsync(string sceneId, CancellationToken cancellationToken = default);
}