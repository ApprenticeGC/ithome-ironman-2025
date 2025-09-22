using Akka.Actor;
using Akka.Configuration;
using GameConsole.AI.Core;
using Microsoft.Extensions.Logging;

namespace GameConsole.AI.Services;

/// <summary>
/// Service for managing Akka.NET actor systems within the GameConsole architecture.
/// Provides lifecycle management and access to the underlying actor system.
/// </summary>
public class ActorSystemService : BaseAIService, IActorSystemService
{
    private ActorSystem? _actorSystem;
    private readonly Config _config;

    /// <summary>
    /// Initializes a new instance of the <see cref="ActorSystemService"/> class.
    /// </summary>
    /// <param name="logger">The logger instance.</param>
    /// <param name="systemName">The name of the actor system.</param>
    /// <param name="config">The Akka.NET configuration.</param>
    public ActorSystemService(ILogger<ActorSystemService> logger, string systemName = "GameConsoleActorSystem", Config? config = null)
        : base(logger)
    {
        SystemName = systemName ?? throw new ArgumentNullException(nameof(systemName));
        _config = config ?? ConfigurationFactory.Default();
    }

    /// <summary>
    /// Gets the name of the actor system.
    /// </summary>
    public string SystemName { get; }

    /// <summary>
    /// Gets a value indicating whether the actor system is running.
    /// </summary>
    public bool IsActorSystemRunning => _actorSystem != null && !_actorSystem.WhenTerminated.IsCompleted;

    /// <summary>
    /// Gets the underlying Akka.NET ActorSystem instance.
    /// </summary>
    internal ActorSystem? ActorSystem => _actorSystem;

    /// <summary>
    /// Creates an actor with the specified name and props.
    /// </summary>
    /// <param name="actorName">The name of the actor to create.</param>
    /// <param name="props">The actor properties.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the actor reference.</returns>
    public async Task<GameConsole.AI.Core.IActorRef> CreateActorAsync(string actorName, GameConsole.AI.Core.Props props)
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system is not initialized. Call InitializeAsync first.");

        if (string.IsNullOrEmpty(actorName))
            throw new ArgumentException("Actor name cannot be null or empty.", nameof(actorName));

        if (props == null)
            throw new ArgumentNullException(nameof(props));

        _logger.LogDebug("Creating actor '{ActorName}' of type {ActorType}", actorName, props.ActorType.Name);

        // Convert our Props interface to Akka.NET Props
        var akkaProps = Akka.Actor.Props.Create(props.ActorType);
        var actorRef = _actorSystem.ActorOf(akkaProps, actorName);

        _logger.LogDebug("Created actor '{ActorName}' at path {ActorPath}", actorName, actorRef.Path);

        return new ActorRefWrapper(actorRef);
    }

    /// <summary>
    /// Gets an actor reference by path.
    /// </summary>
    /// <param name="actorPath">The path to the actor.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the actor reference.</returns>
    public async Task<GameConsole.AI.Core.IActorRef?> GetActorAsync(string actorPath)
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system is not initialized. Call InitializeAsync first.");

        if (string.IsNullOrEmpty(actorPath))
            throw new ArgumentException("Actor path cannot be null or empty.", nameof(actorPath));

        _logger.LogDebug("Getting actor at path '{ActorPath}'", actorPath);

        try
        {
            var selection = _actorSystem.ActorSelection(actorPath);
            var resolveResult = await selection.ResolveOne(TimeSpan.FromSeconds(5));
            
            _logger.LogDebug("Found actor at path '{ActorPath}'", actorPath);
            return new ActorRefWrapper(resolveResult);
        }
        catch (ActorNotFoundException)
        {
            _logger.LogDebug("Actor not found at path '{ActorPath}'", actorPath);
            return null;
        }
    }

    /// <summary>
    /// Terminates the actor system gracefully.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A task representing the async termination operation.</returns>
    public async Task TerminateAsync(CancellationToken cancellationToken = default)
    {
        if (_actorSystem != null)
        {
            _logger.LogInformation("Terminating actor system '{SystemName}'", SystemName);
            
            await _actorSystem.Terminate();
            await _actorSystem.WhenTerminated;
            
            _logger.LogInformation("Actor system '{SystemName}' terminated", SystemName);
            _actorSystem = null;
        }
    }

    protected override async Task OnInitializeAsync(CancellationToken cancellationToken)
    {
        _logger.LogDebug("Initializing actor system '{SystemName}'", SystemName);
        
        _actorSystem = ActorSystem.Create(SystemName, _config);
        
        _logger.LogDebug("Actor system '{SystemName}' initialized", SystemName);
        await Task.CompletedTask;
    }

    protected override async Task OnStartAsync(CancellationToken cancellationToken)
    {
        if (_actorSystem == null)
            throw new InvalidOperationException("Actor system is not initialized.");

        _logger.LogDebug("Actor system '{SystemName}' started", SystemName);
        await Task.CompletedTask;
    }

    protected override async Task OnStopAsync(CancellationToken cancellationToken)
    {
        await TerminateAsync(cancellationToken);
    }

    protected override async ValueTask OnDisposeAsync()
    {
        await TerminateAsync();
    }
}