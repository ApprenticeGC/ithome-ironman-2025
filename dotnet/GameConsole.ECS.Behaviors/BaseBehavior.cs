using Microsoft.Extensions.Logging;

namespace GameConsole.ECS.Behaviors;

/// <summary>
/// Base implementation of a behavior in the ECS behavior composition system.
/// Provides common functionality for all behaviors.
/// </summary>
public abstract class BaseBehavior : IBehavior, IAsyncDisposable
{
    private readonly ILogger _logger;
    private BehaviorState _state = BehaviorState.Inactive;
    private bool _disposed;

    /// <inheritdoc />
    public Guid Id { get; }

    /// <inheritdoc />
    public abstract string Name { get; }

    /// <inheritdoc />
    public BehaviorState State => _state;

    /// <inheritdoc />
    public abstract IReadOnlyCollection<object> Components { get; }

    /// <inheritdoc />
    public abstract IBehaviorMetadata Metadata { get; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseBehavior"/> class.
    /// </summary>
    /// <param name="logger">Logger for this behavior.</param>
    protected BaseBehavior(ILogger logger)
    {
        Id = Guid.NewGuid();
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <inheritdoc />
    public virtual async Task ActivateAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_state == BehaviorState.Active)
        {
            _logger.LogWarning("Behavior {BehaviorName} ({BehaviorId}) is already active", Name, Id);
            return;
        }

        try
        {
            _logger.LogDebug("Activating behavior {BehaviorName} ({BehaviorId})", Name, Id);
            _state = BehaviorState.Active;
            
            await OnActivateAsync(cancellationToken);
            
            _logger.LogInformation("Activated behavior {BehaviorName} ({BehaviorId})", Name, Id);
        }
        catch (Exception ex)
        {
            _state = BehaviorState.Faulted;
            _logger.LogError(ex, "Failed to activate behavior {BehaviorName} ({BehaviorId})", Name, Id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task DeactivateAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_state == BehaviorState.Inactive)
        {
            _logger.LogWarning("Behavior {BehaviorName} ({BehaviorId}) is already inactive", Name, Id);
            return;
        }

        try
        {
            _logger.LogDebug("Deactivating behavior {BehaviorName} ({BehaviorId})", Name, Id);
            
            await OnDeactivateAsync(cancellationToken);
            
            _state = BehaviorState.Inactive;
            _logger.LogInformation("Deactivated behavior {BehaviorName} ({BehaviorId})", Name, Id);
        }
        catch (Exception ex)
        {
            _state = BehaviorState.Faulted;
            _logger.LogError(ex, "Failed to deactivate behavior {BehaviorName} ({BehaviorId})", Name, Id);
            throw;
        }
    }

    /// <inheritdoc />
    public virtual async Task UpdateAsync(TimeSpan deltaTime, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        
        if (_state != BehaviorState.Active)
        {
            return;
        }

        try
        {
            await OnUpdateAsync(deltaTime, cancellationToken);
        }
        catch (Exception ex)
        {
            _state = BehaviorState.Faulted;
            _logger.LogError(ex, "Error updating behavior {BehaviorName} ({BehaviorId})", Name, Id);
            throw;
        }
    }

    /// <summary>
    /// Override this method to implement custom activation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    protected virtual Task OnActivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to implement custom deactivation logic.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    protected virtual Task OnDeactivateAsync(CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override this method to implement custom update logic.
    /// </summary>
    /// <param name="deltaTime">The time elapsed since the last update.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async operation.</returns>
    protected virtual Task OnUpdateAsync(TimeSpan deltaTime, CancellationToken cancellationToken = default)
    {
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        try
        {
            _state = BehaviorState.Disposing;
            _logger.LogDebug("Disposing behavior {BehaviorName} ({BehaviorId})", Name, Id);

            if (_state == BehaviorState.Active)
            {
                await DeactivateAsync();
            }

            await OnDisposeAsync();
            
            _logger.LogInformation("Disposed behavior {BehaviorName} ({BehaviorId})", Name, Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disposing behavior {BehaviorName} ({BehaviorId})", Name, Id);
        }
        finally
        {
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Override this method to implement custom disposal logic.
    /// </summary>
    /// <returns>A task representing the async operation.</returns>
    protected virtual ValueTask OnDisposeAsync()
    {
        return ValueTask.CompletedTask;
    }

    /// <summary>
    /// Throws an <see cref="ObjectDisposedException"/> if this behavior has been disposed.
    /// </summary>
    protected void ThrowIfDisposed()
    {
        if (_disposed)
        {
            throw new ObjectDisposedException(GetType().Name);
        }
    }

    /// <summary>
    /// Gets the logger for this behavior.
    /// </summary>
    protected ILogger Logger => _logger;
}

/// <summary>
/// Basic implementation of behavior metadata.
/// </summary>
public class BehaviorMetadata : IBehaviorMetadata
{
    /// <inheritdoc />
    public IReadOnlyCollection<Type> RequiredComponents { get; init; } = Array.Empty<Type>();

    /// <inheritdoc />
    public IReadOnlyCollection<Type> OptionalComponents { get; init; } = Array.Empty<Type>();

    /// <inheritdoc />
    public IReadOnlyCollection<Type> ConflictingComponents { get; init; } = Array.Empty<Type>();

    /// <inheritdoc />
    public IReadOnlyCollection<Type> Dependencies { get; init; } = Array.Empty<Type>();

    /// <inheritdoc />
    public IReadOnlyCollection<string> Tags { get; init; } = Array.Empty<string>();

    /// <inheritdoc />
    public IReadOnlyDictionary<string, object> Properties { get; init; } = new Dictionary<string, object>();
}