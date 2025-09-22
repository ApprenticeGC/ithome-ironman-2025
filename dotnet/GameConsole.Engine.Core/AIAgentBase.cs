using GameConsole.Core.Abstractions;
using GameConsole.Engine.Core;
using GameConsole.Plugins.Core;

namespace GameConsole.Engine.Core;

/// <summary>
/// Abstract base class for AI agents that combines actor functionality with plugin lifecycle.
/// Provides common behavior management and state tracking for game AI entities.
/// </summary>
public abstract class AIAgentBase : ActorBase, IAIAgent
{
    private string _currentState;
    private readonly object _stateLock = new();

    /// <inheritdoc/>
    public abstract IPluginMetadata Metadata { get; }

    /// <inheritdoc/>
    public IPluginContext? Context { get; set; }

    /// <inheritdoc/>
    public abstract string BehaviorType { get; }

    /// <inheritdoc/>
    public string CurrentState
    {
        get
        {
            lock (_stateLock)
            {
                return _currentState;
            }
        }
        protected set
        {
            string previousState;
            lock (_stateLock)
            {
                previousState = _currentState;
                _currentState = value;
            }

            if (previousState != value)
            {
                OnBehaviorStateChanged(previousState, value);
            }
        }
    }

    /// <inheritdoc/>
    public event EventHandler<BehaviorStateChangedEventArgs>? BehaviorStateChanged;

    /// <summary>
    /// Initializes a new AI agent with the specified initial state.
    /// </summary>
    /// <param name="initialState">The initial behavior state of the agent.</param>
    /// <param name="actorId">Optional actor ID. If not provided, generates a new ID.</param>
    protected AIAgentBase(string initialState, ActorId? actorId = null) : base(actorId)
    {
        _currentState = initialState ?? throw new ArgumentNullException(nameof(initialState));
    }

    /// <inheritdoc/>
    public virtual async Task ConfigureAsync(IPluginContext context, CancellationToken cancellationToken = default)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));
        await OnConfigureAsync(context, cancellationToken);
    }

    /// <inheritdoc/>
    public virtual Task<bool> CanUnloadAsync(CancellationToken cancellationToken = default)
    {
        // By default, allow unloading if the agent is not running
        return Task.FromResult(!IsRunning);
    }

    /// <inheritdoc/>
    public virtual async Task PrepareUnloadAsync(CancellationToken cancellationToken = default)
    {
        if (IsRunning)
        {
            await StopAsync(cancellationToken);
        }
        
        await OnPrepareUnloadAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public abstract Task UpdateBehaviorAsync(double deltaTime, CancellationToken cancellationToken = default);

    /// <summary>
    /// Changes the agent's behavior state with an optional reason.
    /// </summary>
    /// <param name="newState">The new behavior state.</param>
    /// <param name="reason">Optional reason for the state change.</param>
    protected void ChangeState(string newState, string? reason = null)
    {
        if (string.IsNullOrWhiteSpace(newState))
            throw new ArgumentException("New state cannot be null or empty", nameof(newState));

        string previousState;
        lock (_stateLock)
        {
            previousState = _currentState;
            _currentState = newState;
        }

        if (previousState != newState)
        {
            OnBehaviorStateChanged(previousState, newState, reason);
        }
    }

    /// <summary>
    /// Called when the behavior state changes. Fires the BehaviorStateChanged event.
    /// </summary>
    protected virtual void OnBehaviorStateChanged(string previousState, string newState, string? reason = null)
    {
        BehaviorStateChanged?.Invoke(this, new BehaviorStateChangedEventArgs(previousState, newState, reason));
    }

    /// <summary>
    /// Virtual method called during plugin configuration. Override to provide custom configuration logic.
    /// </summary>
    protected virtual Task OnConfigureAsync(IPluginContext context, CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Virtual method called during plugin unload preparation. Override to provide custom cleanup logic.
    /// </summary>
    protected virtual Task OnPrepareUnloadAsync(CancellationToken cancellationToken)
    {
        return Task.CompletedTask;
    }

    /// <summary>
    /// Override to provide type-safe message handling for specific AI agent message types.
    /// Default implementation handles basic coordination messages.
    /// </summary>
    protected override async Task<ActorMessageHandleResult> HandleMessageAsync(IActorMessage message, CancellationToken cancellationToken)
    {
        return await OnHandleMessageAsync(message, cancellationToken);
    }

    /// <summary>
    /// Virtual method for derived classes to handle specific message types.
    /// Override to provide custom message handling logic.
    /// </summary>
    protected virtual Task<ActorMessageHandleResult> OnHandleMessageAsync(IActorMessage message, CancellationToken cancellationToken)
    {
        return Task.FromResult(ActorMessageHandleResult.NotHandled);
    }
}